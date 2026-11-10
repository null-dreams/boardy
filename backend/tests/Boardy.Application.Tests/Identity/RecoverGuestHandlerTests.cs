namespace Boardy.Application.Tests.Identity;

using Boardy.Application.Identity;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Domain.Users;

public class RecoverGuestHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeGuestRecoveryCredentialStore _credentialStore;
    private readonly CryptographicRecoveryCredentialGenerator _credentialGenerator;
    private readonly Sha256RecoveryCredentialHasher _credentialHasher;
    private readonly CreateGuestHandler _createHandler;
    private readonly RecoverGuestHandler _recoverHandler;

    public RecoverGuestHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _userRepository = new FakeUserRepository(_unitOfWork);
        _credentialStore = new FakeGuestRecoveryCredentialStore(_unitOfWork);
        _credentialGenerator = new CryptographicRecoveryCredentialGenerator();
        _credentialHasher = new Sha256RecoveryCredentialHasher();

        _createHandler = new CreateGuestHandler(
            _userRepository,
            _credentialStore,
            _credentialGenerator,
            _credentialHasher,
            _unitOfWork);

        _recoverHandler = new RecoverGuestHandler(
            _userRepository,
            _credentialStore,
            _credentialHasher);
    }

    [Fact]
    public async Task HandleAsync_RecoversExistingGuestUser_WithValidCredential()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Grace"));
        Assert.True(created.IsSuccess);

        var query = new RecoverGuestQuery(created.Value.RecoveryCredential);
        var recovered = await _recoverHandler.HandleAsync(query);

        Assert.True(recovered.IsSuccess);
        Assert.Equal(created.Value.User.Id, recovered.Value.Id);
        Assert.Equal("Grace", recovered.Value.DisplayName);
        Assert.Equal(UserKind.Guest, recovered.Value.Kind);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenCredentialIsEmptyOrWhitespace(string invalidCredential)
    {
        var query = new RecoverGuestQuery(invalidCredential);
        var result = await _recoverHandler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenQueryIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _recoverHandler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenCredentialNotFound()
    {
        var query = new RecoverGuestQuery("non-existent-recovery-credential");
        var result = await _recoverHandler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenCredentialTamperedWith()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Heidi"));
        var tamperedCredential = created.Value.RecoveryCredential + "_tampered";

        var query = new RecoverGuestQuery(tamperedCredential);
        var result = await _recoverHandler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenUserNoLongerExistsInRepository()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Ivan"));
        // Simulate user deletion from repository while credential remains
        _userRepository.CommittedUsers.Remove(created.Value.User.Id);

        var query = new RecoverGuestQuery(created.Value.RecoveryCredential);
        var result = await _recoverHandler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenUserIsRegisteredRatherThanGuest()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Judy"));
        // Upgrade user to registered and persist
        created.Value.User.UpgradeToRegistered();
        await _userRepository.UpdateAsync(created.Value.User);
        await _unitOfWork.SaveChangesAsync();

        var query = new RecoverGuestQuery(created.Value.RecoveryCredential);
        var result = await _recoverHandler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_DoesNotExposeWhetherUserIdExists()
    {
        // Both non-existent credential and orphaned credential return the exact same error code and description
        var nonExistentResult = await _recoverHandler.HandleAsync(new RecoverGuestQuery("completely-random-credential-12345"));

        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Ken"));
        _userRepository.CommittedUsers.Remove(created.Value.User.Id); // orphaned
        var orphanedResult = await _recoverHandler.HandleAsync(new RecoverGuestQuery(created.Value.RecoveryCredential));

        Assert.True(nonExistentResult.IsFailure);
        Assert.True(orphanedResult.IsFailure);
        Assert.Equal(nonExistentResult.Error, orphanedResult.Error);
    }
}
