namespace Boardy.Application.Identity;

using Boardy.Application.Common.Results;
using Boardy.Domain.Users;

public sealed class RecoverGuestHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IGuestRecoveryCredentialStore _credentialStore;
    private readonly IRecoveryCredentialHasher _credentialHasher;

    public RecoverGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore)
        : this(userRepository, credentialStore, new Sha256RecoveryCredentialHasher())
    {
    }

    public RecoverGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore,
        IRecoveryCredentialHasher credentialHasher)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _credentialHasher = credentialHasher ?? throw new ArgumentNullException(nameof(credentialHasher));
    }

    public async Task<Result<User>> HandleAsync(
        RecoverGuestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.RecoveryCredential))
        {
            return Result.Failure<User>(GuestIdentityErrors.InvalidRecoveryCredential);
        }

        var credentialHash = _credentialHasher.HashCredential(query.RecoveryCredential.Trim());

        var userId = await _credentialStore.FindUserIdByCredentialHashAsync(credentialHash, cancellationToken);
        if (userId is null)
        {
            return Result.Failure<User>(GuestIdentityErrors.InvalidRecoveryCredential);
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || user.Kind != UserKind.Guest)
        {
            return Result.Failure<User>(GuestIdentityErrors.InvalidRecoveryCredential);
        }

        return Result.Success(user);
    }
}
