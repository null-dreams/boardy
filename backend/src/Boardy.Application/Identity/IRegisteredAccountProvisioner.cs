namespace Boardy.Application.Identity;

using Boardy.Application.Common.Results;

public sealed record ProvisionedAccount(string AccountId, string Email);

public interface IRegisteredAccountProvisioner
{
    Task<Result<ProvisionedAccount>> ProvisionAccountAsync(
        Guid userId,
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result> RollbackAccountAsync(
        Guid userId,
        ProvisionedAccount provisionedAccount,
        CancellationToken cancellationToken = default);
}
