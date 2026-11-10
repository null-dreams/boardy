namespace Boardy.Application.Tests.Identity;

using Boardy.Application.Common.Results;
using Boardy.Application.Identity;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Domain.Users;

public class UpgradeGuestHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeGuestRecoveryCredentialStore _credentialStore;
    private readonly FakeRegisteredAccountProvisioner _accountProvisioner;
    private readonly CryptographicRecoveryCredentialGenerator _credentialGenerator;
    private readonly Sha256RecoveryCredentialHasher _credentialHasher;
    private readonly CreateGuestHandler _createHandler;
    private readonly RecoverGuestHandler _recoverHandler;
    private readonly UpgradeGuestHandler _upgradeHandler;

    public UpgradeGuestHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _userRepository = new FakeUserRepository(_unitOfWork);
        _credentialStore = new FakeGuestRecoveryCredentialStore(_unitOfWork);
        _accountProvisioner = new FakeRegisteredAccountProvisioner();
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

        _upgradeHandler = new UpgradeGuestHandler(
            _userRepository,
            _credentialStore,
            _accountProvisioner,
            _credentialHasher,
            _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_SuccessfullyUpgradesGuest_PreservingIdCreatedAtAndDisplayName_ViaAuthenticatedIdentity()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Alice"));
        Assert.True(created.IsSuccess);
        var originalUser = created.Value.User;

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "alice@example.com",
            password: "SecurePassword123!",
            authenticatedUserId: originalUser.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var upgradedUser = result.Value;

        // Invariant: User.Id, DisplayName, and CreatedAt are strictly preserved
        Assert.Equal(originalUser.Id, upgradedUser.Id);
        Assert.Equal(originalUser.DisplayName, upgradedUser.DisplayName);
        Assert.Equal(originalUser.CreatedAt, upgradedUser.CreatedAt);
        Assert.Equal(UserKind.Registered, upgradedUser.Kind);

        // Verify persisted user in repository
        var persisted = await _userRepository.GetByIdAsync(originalUser.Id);
        Assert.NotNull(persisted);
        Assert.Equal(UserKind.Registered, persisted.Kind);
    }

    [Fact]
    public async Task HandleAsync_SuccessfullyUpgradesGuest_UsingRecoveryCredential()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Bob"));
        Assert.True(created.IsSuccess);

        var command = UpgradeGuestCommand.FromRecoveryCredential(
            email: "bob@example.com",
            password: "SecurePassword123!",
            recoveryCredential: created.Value.RecoveryCredential);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.User.Id, result.Value.Id);
        Assert.Equal(UserKind.Registered, result.Value.Kind);
    }

    [Fact]
    public async Task HandleAsync_SuccessfullyUpgradesGuest_UsingBothMatchingIdentifiers()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Charlie"));
        Assert.True(created.IsSuccess);

        var command = UpgradeGuestCommand.FromRecoveryCredential(
            email: "charlie@example.com",
            password: "SecurePassword123!",
            recoveryCredential: created.Value.RecoveryCredential,
            expectedGuestUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Value.User.Id, result.Value.Id);
        Assert.Equal(UserKind.Registered, result.Value.Kind);
    }

    [Fact]
    public async Task HandleAsync_RejectsCallerSuppliedUserId_WhenNotAuthenticated_AndNoCredentialSupplied()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("UntrustedCaller"));
        Assert.True(created.IsSuccess);

        // Caller passes bare GuestUserId without IsAuthenticatedIdentity and without RecoveryCredential
        var command = new UpgradeGuestCommand(
            Email: "attacker@example.com",
            Password: "SecurePassword123!",
            GuestUserId: created.Value.User.Id,
            RecoveryCredential: null,
            IsAuthenticatedIdentity: false);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.ProofOfOwnershipRequired, result.Error);

        // User must remain an un-upgraded guest
        var persisted = await _userRepository.GetByIdAsync(created.Value.User.Id);
        Assert.NotNull(persisted);
        Assert.Equal(UserKind.Guest, persisted.Kind);
    }

    [Fact]
    public async Task HandleAsync_InvalidatesGuestRecoveryCredential_UponSuccessfulUpgrade()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Dave"));
        Assert.True(created.IsSuccess);
        var rawCredential = created.Value.RecoveryCredential;

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "dave@example.com",
            password: "SecurePassword123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);
        Assert.True(result.IsSuccess);

        // Verification 1: Credential store no longer has credential hash for this user
        var credentialHash = _credentialHasher.HashCredential(rawCredential);
        var foundUserId = await _credentialStore.FindUserIdByCredentialHashAsync(credentialHash);
        Assert.Null(foundUserId);

        // Verification 2: RecoverGuestHandler rejects the old credential
        var recoveryResult = await _recoverHandler.HandleAsync(new RecoverGuestQuery(rawCredential));
        Assert.True(recoveryResult.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, recoveryResult.Error);
    }

    [Fact]
    public async Task HandleAsync_RejectsAlreadyRegisteredUser()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Eve"));
        Assert.True(created.IsSuccess);

        var command1 = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "eve@example.com",
            password: "SecurePassword123!",
            authenticatedUserId: created.Value.User.Id);

        var result1 = await _upgradeHandler.HandleAsync(command1);
        Assert.True(result1.IsSuccess);

        // Repeated upgrade attempt for the same already-registered user
        var command2 = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "eve-alternate@example.com",
            password: "NewSecurePassword123!",
            authenticatedUserId: created.Value.User.Id);

        var result2 = await _upgradeHandler.HandleAsync(command2);

        Assert.True(result2.IsFailure);
        Assert.Equal(GuestIdentityErrors.UserAlreadyRegistered, result2.Error);
        Assert.False(_accountProvisioner.UserIdsByEmail.ContainsKey("eve-alternate@example.com"));
    }

    [Fact]
    public async Task HandleAsync_RejectsDuplicateUpgradeAttempt_UsingInvalidatedRecoveryCredential()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Frank"));
        Assert.True(created.IsSuccess);

        var command1 = UpgradeGuestCommand.FromRecoveryCredential(
            email: "frank@example.com",
            password: "SecurePassword123!",
            recoveryCredential: created.Value.RecoveryCredential);

        var result1 = await _upgradeHandler.HandleAsync(command1);
        Assert.True(result1.IsSuccess);

        // Attempting to upgrade again using the same recovery credential
        var command2 = UpgradeGuestCommand.FromRecoveryCredential(
            email: "frank2@example.com",
            password: "SecurePassword123!",
            recoveryCredential: created.Value.RecoveryCredential);

        var result2 = await _upgradeHandler.HandleAsync(command2);

        Assert.True(result2.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result2.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public async Task HandleAsync_ReturnsInvalidEmail_WhenEmailIsEmptyOrWhitespace(string invalidEmail)
    {
        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: invalidEmail,
            password: "Password123!",
            authenticatedUserId: Guid.NewGuid());

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidEmail, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public async Task HandleAsync_ReturnsInvalidPassword_WhenPasswordIsEmptyOrWhitespace(string invalidPassword)
    {
        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "user@example.com",
            password: invalidPassword,
            authenticatedUserId: Guid.NewGuid());

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidPassword, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsMissingGuestIdentifier_WhenNeitherUserIdNorCredentialProvided()
    {
        var command = new UpgradeGuestCommand(
            Email: "user@example.com",
            Password: "Password123!",
            GuestUserId: null,
            RecoveryCredential: null,
            IsAuthenticatedIdentity: false);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.MissingGuestIdentifier, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGuestUserNotFound_WhenGuestUserIdDoesNotExist()
    {
        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "user@example.com",
            password: "Password123!",
            authenticatedUserId: Guid.NewGuid());

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.GuestUserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsInvalidRecoveryCredential_WhenCredentialNotFound()
    {
        var command = UpgradeGuestCommand.FromRecoveryCredential(
            email: "user@example.com",
            password: "Password123!",
            recoveryCredential: "non-existent-recovery-credential");

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.InvalidRecoveryCredential, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGuestIdentityMismatch_WhenUserIdAndCredentialBelongToDifferentUsers()
    {
        var guest1 = await _createHandler.HandleAsync(new CreateGuestCommand("Guest1"));
        var guest2 = await _createHandler.HandleAsync(new CreateGuestCommand("Guest2"));

        var command = UpgradeGuestCommand.FromRecoveryCredential(
            email: "mismatch@example.com",
            password: "Password123!",
            recoveryCredential: guest2.Value.RecoveryCredential, // belongs to guest2
            expectedGuestUserId: guest1.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.GuestIdentityMismatch, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsProvisioningError_AndPreservesGuest_WhenAccountProvisionerFails()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Grace"));
        Assert.True(created.IsSuccess);

        _accountProvisioner.ShouldFailProvisioning = true;
        _accountProvisioner.FailureError = new Error("Auth.DuplicateEmail", "Email already in use.");

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "grace@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Auth.DuplicateEmail", result.Error.Code);

        // Verification: User in repository is STILL a guest
        var persistedUser = await _userRepository.GetByIdAsync(created.Value.User.Id);
        Assert.NotNull(persistedUser);
        Assert.Equal(UserKind.Guest, persistedUser.Kind);

        // Verification: Recovery credential is STILL valid
        var recoveryResult = await _recoverHandler.HandleAsync(new RecoverGuestQuery(created.Value.RecoveryCredential));
        Assert.True(recoveryResult.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_RollsBackProvisionedAccount_AndPreservesGuest_WhenDomainPersistenceFails()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Heidi"));
        Assert.True(created.IsSuccess);

        _unitOfWork.ShouldFail = true;

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "heidi@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(GuestIdentityErrors.PersistenceFailed, result.Error);

        // Verification 1: Compensation executed (provisioned account rolled back)
        Assert.Equal(1, _accountProvisioner.RollbackCallCount);
        Assert.NotNull(_accountProvisioner.LastRolledBackAccount);
        Assert.Equal("heidi@example.com", _accountProvisioner.LastRolledBackAccount.Email);
        Assert.False(_accountProvisioner.AccountsByUserId.ContainsKey(created.Value.User.Id));
        Assert.False(_accountProvisioner.UserIdsByEmail.ContainsKey("heidi@example.com"));

        // Verification 2: User in repository remains a Guest
        var persistedUser = await _userRepository.GetByIdAsync(created.Value.User.Id);
        Assert.NotNull(persistedUser);
        Assert.Equal(UserKind.Guest, persistedUser.Kind);

        // Verification 3: Recovery credential was NOT deleted from committed storage
        var credentialHash = _credentialHasher.HashCredential(created.Value.RecoveryCredential);
        var foundUserId = await _credentialStore.FindUserIdByCredentialHashAsync(credentialHash);
        Assert.Equal(created.Value.User.Id, foundUserId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCompensationFailed_WhenPersistenceAndRollbackBothFail()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Ian"));
        Assert.True(created.IsSuccess);

        _unitOfWork.ShouldFail = true;
        _accountProvisioner.ShouldFailRollback = true;
        _accountProvisioner.RollbackError = new Error("Auth.NetworkTimeout", "External auth service timed out during rollback.");

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "ian@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Guest.CompensationFailed", result.Error.Code);
        Assert.Contains(created.Value.User.Id.ToString(), result.Error.Description);
        Assert.Contains("External auth service timed out during rollback.", result.Error.Description);

        // Ensure sensitive data like password is NEVER present in the error message
        Assert.DoesNotContain("Password123!", result.Error.Description);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCompensationFailed_WhenRollbackThrowsUnexpectedException()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Jack"));
        Assert.True(created.IsSuccess);

        _unitOfWork.ShouldFail = true;
        _accountProvisioner.ShouldThrowOnRollback = true;

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "jack@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal("Guest.CompensationFailed", result.Error.Code);
        Assert.Contains("External identity provider connection refused", result.Error.Description);
        Assert.DoesNotContain("Password123!", result.Error.Description);
    }

    [Fact]
    public async Task HandleAsync_RollbackOnlyRemovesAccountCreatedByCurrentOperation()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Karen"));
        Assert.True(created.IsSuccess);

        _unitOfWork.ShouldFail = true;

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "karen@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);

        Assert.True(result.IsFailure);

        // Verify that the provisioner received the specific ProvisionedAccount token
        Assert.NotNull(_accountProvisioner.LastRolledBackAccount);
        Assert.Equal("karen@example.com", _accountProvisioner.LastRolledBackAccount.Email);
        Assert.StartsWith("acc-", _accountProvisioner.LastRolledBackAccount.AccountId);
    }

    [Fact]
    public async Task HandleAsync_EnsuresConsistencyBetweenAuthenticationIdentityAndPlatformIdentity()
    {
        var created = await _createHandler.HandleAsync(new CreateGuestCommand("Leo"));
        Assert.True(created.IsSuccess);

        var command = UpgradeGuestCommand.FromAuthenticatedIdentity(
            email: "leo@example.com",
            password: "Password123!",
            authenticatedUserId: created.Value.User.Id);

        var result = await _upgradeHandler.HandleAsync(command);
        Assert.True(result.IsSuccess);

        // The authentication store has an exact mapping: UserId -> ProvisionedAccount
        Assert.True(_accountProvisioner.AccountsByUserId.ContainsKey(created.Value.User.Id));
        Assert.Equal("leo@example.com", _accountProvisioner.AccountsByUserId[created.Value.User.Id].Email);
        Assert.Equal(created.Value.User.Id, _accountProvisioner.UserIdsByEmail["leo@example.com"]);
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenCommandIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _upgradeHandler.HandleAsync(null!));
    }
}
