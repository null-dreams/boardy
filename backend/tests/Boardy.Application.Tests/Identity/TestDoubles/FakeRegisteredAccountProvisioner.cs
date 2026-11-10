namespace Boardy.Application.Tests.Identity.TestDoubles;

using Boardy.Application.Common.Results;
using Boardy.Application.Identity;

internal sealed class FakeRegisteredAccountProvisioner : IRegisteredAccountProvisioner
{
    public Dictionary<Guid, ProvisionedAccount> AccountsByUserId { get; } = [];
    public Dictionary<string, Guid> UserIdsByEmail { get; } = [];

    public bool ShouldFailProvisioning { get; set; }
    public Error FailureError { get; set; } = new("Auth.ProvisioningFailed", "Account provisioning failed.");

    public bool ShouldFailRollback { get; set; }
    public bool ShouldThrowOnRollback { get; set; }
    public Error RollbackError { get; set; } = new("Auth.RollbackFailed", "External rollback endpoint returned error.");

    public int RollbackCallCount { get; private set; }
    public ProvisionedAccount? LastRolledBackAccount { get; private set; }

    public Task<Result<ProvisionedAccount>> ProvisionAccountAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFailProvisioning)
        {
            return Task.FromResult(Result.Failure<ProvisionedAccount>(FailureError));
        }

        if (AccountsByUserId.ContainsKey(userId))
        {
            return Task.FromResult(Result.Failure<ProvisionedAccount>(new Error("Auth.DuplicateUserAccount", "User already has an authentication account.")));
        }

        if (UserIdsByEmail.ContainsKey(email))
        {
            return Task.FromResult(Result.Failure<ProvisionedAccount>(new Error("Auth.DuplicateEmail", "Email is already registered.")));
        }

        var account = new ProvisionedAccount($"acc-{Guid.NewGuid():N}", email);
        AccountsByUserId[userId] = account;
        UserIdsByEmail[email] = userId;

        return Task.FromResult(Result.Success(account));
    }

    public Task<Result> RollbackAccountAsync(
        Guid userId,
        ProvisionedAccount provisionedAccount,
        CancellationToken cancellationToken = default)
    {
        RollbackCallCount++;
        LastRolledBackAccount = provisionedAccount;

        if (ShouldThrowOnRollback)
        {
            throw new InvalidOperationException("External identity provider connection refused during rollback.");
        }

        if (ShouldFailRollback)
        {
            return Task.FromResult(Result.Failure(RollbackError));
        }

        if (AccountsByUserId.TryGetValue(userId, out var account) && account.AccountId == provisionedAccount.AccountId)
        {
            UserIdsByEmail.Remove(account.Email);
            AccountsByUserId.Remove(userId);
            return Task.FromResult(Result.Success());
        }

        return Task.FromResult(Result.Failure(new Error("Auth.AccountMismatch", "Account not found or does not match provisioned account ID.")));
    }
}
