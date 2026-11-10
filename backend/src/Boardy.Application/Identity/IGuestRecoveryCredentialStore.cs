namespace Boardy.Application.Identity;

public interface IGuestRecoveryCredentialStore
{
    Task SaveCredentialAsync(Guid userId, string credentialHash, CancellationToken cancellationToken = default);
    Task<Guid?> FindUserIdByCredentialHashAsync(string credentialHash, CancellationToken cancellationToken = default);
    Task InvalidateCredentialsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
