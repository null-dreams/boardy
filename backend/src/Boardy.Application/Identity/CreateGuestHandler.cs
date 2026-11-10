namespace Boardy.Application.Identity;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Users;

public sealed class CreateGuestHandler
{
    private readonly IUserRepository _userRepository;
    private readonly IGuestRecoveryCredentialStore _credentialStore;
    private readonly IRecoveryCredentialGenerator _credentialGenerator;
    private readonly IRecoveryCredentialHasher _credentialHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore,
        IUnitOfWork unitOfWork)
        : this(
            userRepository,
            credentialStore,
            new CryptographicRecoveryCredentialGenerator(),
            new Sha256RecoveryCredentialHasher(),
            unitOfWork)
    {
    }

    public CreateGuestHandler(
        IUserRepository userRepository,
        IGuestRecoveryCredentialStore credentialStore,
        IRecoveryCredentialGenerator credentialGenerator,
        IRecoveryCredentialHasher credentialHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _credentialGenerator = credentialGenerator ?? throw new ArgumentNullException(nameof(credentialGenerator));
        _credentialHasher = credentialHasher ?? throw new ArgumentNullException(nameof(credentialHasher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<CreateGuestResult>> HandleAsync(
        CreateGuestCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.DisplayName))
        {
            return Result.Failure<CreateGuestResult>(GuestIdentityErrors.InvalidDisplayName);
        }

        var user = User.CreateGuest(command.DisplayName);
        var rawCredential = _credentialGenerator.GenerateCredential();

        if (string.IsNullOrWhiteSpace(rawCredential))
        {
            return Result.Failure<CreateGuestResult>(GuestIdentityErrors.CredentialGenerationFailed);
        }

        var credentialHash = _credentialHasher.HashCredential(rawCredential);

        try
        {
            await _userRepository.AddAsync(user, cancellationToken);
            await _credentialStore.SaveCredentialAsync(user.Id, credentialHash, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<CreateGuestResult>(GuestIdentityErrors.PersistenceFailed);
        }

        return Result.Success(new CreateGuestResult(user, rawCredential));
    }
}
