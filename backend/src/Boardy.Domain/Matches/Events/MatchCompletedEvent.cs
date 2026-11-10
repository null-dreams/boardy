using Boardy.Domain.Events;

namespace Boardy.Domain.Matches.Events;

public sealed record MatchCompletedEvent : DomainEvent
{
    public Guid MatchId { get; }
    public MatchResult Result { get; }

    public MatchCompletedEvent(Guid matchId, MatchResult result)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        ArgumentNullException.ThrowIfNull(result);

        MatchId = matchId;
        Result = result;
    }
}
