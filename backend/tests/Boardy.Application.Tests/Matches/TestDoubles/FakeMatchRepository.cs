namespace Boardy.Application.Tests.Matches.TestDoubles;

using Boardy.Application.Matches;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Domain.Matches;

internal sealed class FakeMatchRepository : IMatchRepository
{
    private readonly FakeUnitOfWork _unitOfWork;

    public Dictionary<Guid, Match> CommittedMatchesById { get; } = [];

    public bool ShouldFailOnAdd { get; set; }
    public bool ShouldFailOnUpdate { get; set; }

    public FakeMatchRepository(FakeUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CommittedMatchesById.TryGetValue(id, out var match);
        return Task.FromResult(match);
    }

    public Task AddAsync(Match match, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFailOnAdd)
        {
            throw new InvalidOperationException("Simulated repository failure on Add.");
        }

        _unitOfWork.RegisterCommitAction(() =>
        {
            CommittedMatchesById[match.Id] = match;
        });

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Match match, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldFailOnUpdate)
        {
            throw new InvalidOperationException("Simulated repository failure on Update.");
        }

        _unitOfWork.RegisterCommitAction(() =>
        {
            CommittedMatchesById[match.Id] = match;
        });

        return Task.CompletedTask;
    }
}
