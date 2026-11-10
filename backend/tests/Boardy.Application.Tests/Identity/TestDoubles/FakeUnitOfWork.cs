namespace Boardy.Application.Tests.Identity.TestDoubles;

using Boardy.Application.Common.Persistence;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _commitActions = [];

    public bool ShouldFail { get; set; }

    public void RegisterCommitAction(Action action)
    {
        _commitActions.Add(action);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
        {
            _commitActions.Clear();
            throw new InvalidOperationException("Simulated database failure during SaveChanges.");
        }

        foreach (var action in _commitActions)
        {
            action();
        }

        _commitActions.Clear();
        return Task.CompletedTask;
    }
}
