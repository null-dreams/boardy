namespace Boardy.Application.Tests.Identity.TestDoubles;

using Boardy.Application.Identity;

internal sealed class FakeGuestRecoveryCredentialStore : IGuestRecoveryCredentialStore
{
    private readonly FakeUnitOfWork _unitOfWork;

    public Dictionary<string, Guid> CommittedCredentials { get; } = []; // credentialHash -> userId
    public bool ShouldFailOnSave { get; set; }

    public FakeGuestRecoveryCredentialStore(FakeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task SaveCredentialAsync(Guid userId, string credentialHash, CancellationToken cancellationToken = default)
    {
        if (ShouldFailOnSave)
        {
            throw new InvalidOperationException("Simulated store failure on SaveCredential.");
        }

        _unitOfWork.RegisterCommitAction(() => CommittedCredentials[credentialHash] = userId);
        return Task.CompletedTask;
    }

    public Task<Guid?> FindUserIdByCredentialHashAsync(string credentialHash, CancellationToken cancellationToken = default)
    {
        if (CommittedCredentials.TryGetValue(credentialHash, out var userId))
        {
            return Task.FromResult<Guid?>(userId);
        }

        return Task.FromResult<Guid?>(null);
    }

    public Task InvalidateCredentialsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _unitOfWork.RegisterCommitAction(() =>
        {
            var keysToRemove = CommittedCredentials
                .Where(kvp => kvp.Value == userId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                CommittedCredentials.Remove(key);
            }
        });

        return Task.CompletedTask;
    }
}
