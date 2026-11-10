namespace Boardy.Application.Tests.Identity.TestDoubles;

using System.Reflection;
using Boardy.Application.Identity;
using Boardy.Domain.Users;

internal sealed class FakeUserRepository : IUserRepository
{
    private static readonly ConstructorInfo UserConstructor = typeof(User)
        .GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(Guid), typeof(string), typeof(UserKind), typeof(DateTimeOffset)],
            null)!;

    private readonly FakeUnitOfWork _unitOfWork;

    public Dictionary<Guid, (string DisplayName, UserKind Kind, DateTimeOffset CreatedAt)> CommittedUsers { get; } = [];
    public bool ShouldFailOnAdd { get; set; }

    public FakeUserRepository(FakeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (ShouldFailOnAdd)
        {
            throw new InvalidOperationException("Simulated repository failure on Add.");
        }

        _unitOfWork.RegisterCommitAction(() => CommittedUsers[user.Id] = (user.DisplayName, user.Kind, user.CreatedAt));
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (CommittedUsers.TryGetValue(id, out var data))
        {
            var user = (User)UserConstructor.Invoke([id, data.DisplayName, data.Kind, data.CreatedAt]);
            return Task.FromResult<User?>(user);
        }

        return Task.FromResult<User?>(null);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _unitOfWork.RegisterCommitAction(() => CommittedUsers[user.Id] = (user.DisplayName, user.Kind, user.CreatedAt));
        return Task.CompletedTask;
    }
}
