namespace Boardy.Application.Matches;

using Boardy.Domain.Matches;

/// <summary>
/// Defines persistence operations for the <see cref="Match"/> aggregate.
/// Implementations must coordinate with other aggregate repositories
/// (such as IRoomRepository) within a shared IUnitOfWork boundary.
/// </summary>
public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Match match, CancellationToken cancellationToken = default);
    Task UpdateAsync(Match match, CancellationToken cancellationToken = default);
}
