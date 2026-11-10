namespace Boardy.Application.Identity;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Users;

public sealed class UpgradeGuestHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IGuestRecoveryCredentialStore _credentialStore;
    private readonly IRegisteredAccountProvisioner _accountProvisioner;
    private readonly IRecoveryCredentialHasher _credentialHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UpgradeGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore,
        IRegisteredAccountProvisioner accountProvisioner,
        IUnitOfWork unitOfWork)
        : this(
            userRepository,
            credentialStore,
            accountProvisioner,
            new Sha256RecoveryCredentialHasher(),
            unitOfWork)
    {
    }

    public UpgradeGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore,
        IRegisteredAccountProvisioner accountProvisioner,
        IRecoveryCredentialHasher credentialHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _accountProvisioner = accountProvisioner ?? throw new ArgumentNullException(nameof(accountProvisioner));
        _credentialHasher = credentialHasher ?? throw new ArgumentNullException(nameof(credentialHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<User>> HandleAsync(
        UpgradeGuestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            return Result.Failure<User>(GuestIdentityErrors.InvalidEmail);
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            return Result.Failure<User>(GuestIdentityErrors.InvalidPassword);
        }

        if (command.GuestUserId is null && string.IsNullOrWhiteSpace(command.RecoveryCredential))
        {
            return Result.Failure<User>(GuestIdentityErrors.MissingGuestIdentifier);
        }

        Guid targetUserId;

        if (!string.IsNullOrWhiteSpace(command.RecoveryCredential))
        {
            var credentialHash = _credentialHasher.HashCredential(command.RecoveryCredential.Trim());
            var userIdFromCredential = await _credentialStore.FindUserIdByCredentialHashAsync(credentialHash, cancellationToken);

            if (userIdFromCredential is null)
            {
                return Result.Failure<User>(GuestIdentityErrors.InvalidRecoveryCredential);
            }

            if (command.GuestUserId is not null && command.GuestUserId.Value != userIdFromCredential.Value)
            {
                return Result.Failure<User>(GuestIdentityErrors.GuestIdentityMismatch);
            }

            targetUserId = userIdFromCredential.Value;
        }
        else
        {
            // A caller-supplied user ID is never treated as proof of ownership unless explicitly marked as authenticated identity
            if (!command.IsAuthenticatedIdentity)
            {
                return Result.Failure<User>(GuestIdentityErrors.ProofOfOwnershipRequired);
            }

            targetUserId = command.GuestUserId!.Value;
        }

        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<User>(GuestIdentityErrors.GuestUserNotFound);
        }

        if (user.Kind != UserKind.Guest)
        {
            return Result.Failure<User>(GuestIdentityErrors.UserAlreadyRegistered);
        }

        var provisionResult = await _accountProvisioner.ProvisionAccountAsync(
            user.Id,
            command.Email.Trim(),
            command.Password,
            cancellationToken);

        if (provisionResult.IsFailure)
        {
            return Result.Failure<User>(provisionResult.Error);
        }

        ProvisionedAccount provisionedAccount = provisionResult.Value;

        try
        {
            user.UpgradeToRegistered();

            await _userRepository.UpdateAsync(user, cancellationToken);
            await _credentialStore.InvalidateCredentialsForUserAsync(user.Id, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistenceEx)
        {
            // Compensation: Roll back the specific account created by this provisioning operation
            Result rollbackResult;
            try
            {
                rollbackResult = await _accountProvisioner.RollbackAccountAsync(
                    user.Id,
                    provisionedAccount,
                    cancellationToken);
            }
            catch (Exception rollbackEx)
            {
                rollbackResult = Result.Failure(new Error(
                    "Guest.CompensationException",
                    $"Compensation rollback threw an exception: {rollbackEx.Message}"));
            }

            if (rollbackResult.IsFailure)
            {
                return Result.Failure<User>(GuestIdentityErrors.CompensationFailed(
                    user.Id,
                    provisionedAccount.AccountId,
                    $"Persistence failed ({persistenceEx.Message}). Rollback error: {rollbackResult.Error.Description}"));
            }

            return Result.Failure<User>(GuestIdentityErrors.PersistenceFailed);
        }

        return Result.Success(user);
    }
}
