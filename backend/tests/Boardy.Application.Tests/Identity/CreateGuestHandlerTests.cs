namespace Boardy.Application.Tests.Identity;

using Boardy.Application.Identity;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Domain.Users;

public class CreateGuestHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeGuestRecoveryCredentialStore _credentialStore;
    private readonly CryptographicRecoveryCredentialGenerator _credentialGenerator;
    private readonly Sha256RecoveryCredentialHasher _credentialHasher;
    private readonly CreateGuestHandler _handler;

    public CreateGuestHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _userRepository = new FakeUserRepository(_unitOfWork);
        _credentialStore = new FakeGuestRecoveryCredentialStore(_unitOfWork);
        _credentialGenerator = new CryptographicRecoveryCredentialGenerator();
        _credentialHasher = new Sha256RecoveryCredentialHasher();

        _handler = new CreateGuestHandler(
            _userRepository,
            _credentialStore,
            _credentialGenerator,
            _credentialHasher,
            _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_CreatesAndPersistsGuestUser_WithValidRecoveryCredential()
    {
        var command = new CreateGuestCommand("Alice");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.User);
        Assert.Equal("Alice", result.Value.User.DisplayName);
        Assert.Equal(UserKind.Guest, result.Value.User.Kind);
        Assert.NotEqual(Guid.Empty, result.Value.User.Id);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RecoveryCredential));

        // Verify user persistence
        var persistedUser = await _userRepository.GetByIdAsync(result.Value.User.Id);
        Assert.NotNull(persistedUser);
        Assert.Equal(result.Value.User.Id, persistedUser.Id);
        Assert.Equal("Alice", persistedUser.DisplayName);
    }

    [Fact]
    public async Task HandleAsync_NeverPersistsRawRecoveryCredential()
    {
        var command = new CreateGuestCommand("Bob");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var rawCredential = result.Value.RecoveryCredential;

        // Raw credential should NOT be a key in the credential store
        Assert.False(_credentialStore.CommittedCredentials.ContainsKey(rawCredential));

        // The hash of the credential MUST be the stored key
        var expectedHash = _credentialHasher.HashCredential(rawCredential);
        Assert.True(_credentialStore.CommittedCredentials.ContainsKey(expectedHash));
        Assert.Equal(result.Value.User.Id, _credentialStore.CommittedCredentials[expectedHash]);
        Assert.NotEqual(rawCredential, expectedHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public async Task HandleAsync_ReturnsInvalidDisplayNameError_WhenDisplayNameIsEmptyOrWhitespace(string invalidDisplayName)
    {
        var command = new CreateGuestCommand(invalidDisplayName);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidDisplayName, result.Error);
        Assert.Empty(_userRepository.CommittedUsers);
        Assert.Empty(_credentialStore.CommittedCredentials);
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenCommandIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_ReturnsPersistenceFailed_AndCommitsNoPartialData_WhenUnitOfWorkFails()
    {
        _unitOfWork.ShouldFail = true;
        var command = new CreateGuestCommand("Charlie");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.PersistenceFailed, result.Error);

        // Verification of atomic failure: NO partial user and NO partial credential committed
        Assert.Empty(_userRepository.CommittedUsers);
        Assert.Empty(_credentialStore.CommittedCredentials);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPersistenceFailed_AndCommitsNoData_WhenUserRepositoryFails()
    {
        _userRepository.ShouldFailOnAdd = true;
        var command = new CreateGuestCommand("Dave");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.PersistenceFailed, result.Error);
        Assert.Empty(_userRepository.CommittedUsers);
        Assert.Empty(_credentialStore.CommittedCredentials);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPersistenceFailed_AndCommitsNoData_WhenCredentialStoreFails()
    {
        _credentialStore.ShouldFailOnSave = true;
        var command = new CreateGuestCommand("Eve");

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.PersistenceFailed, result.Error);
        Assert.Empty(_userRepository.CommittedUsers);
        Assert.Empty(_credentialStore.CommittedCredentials);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCredentialGenerationFailed_WhenGeneratorReturnsEmpty()
    {
        var stubGenerator = new StubEmptyCredentialGenerator();
        var handler = new CreateGuestHandler(
            _userRepository,
            _credentialStore,
            stubGenerator,
            _credentialHasher,
            _unitOfWork);

        var result = await handler.HandleAsync(new CreateGuestCommand("Frank"));

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.CredentialGenerationFailed, result.Error);
        Assert.Empty(_userRepository.CommittedUsers);
        Assert.Empty(_credentialStore.CommittedCredentials);
    }

    private sealed class StubEmptyCredentialGenerator : IRecoveryCredentialGenerator
    {
        public string GenerateCredential() => string.Empty;
    }
}
