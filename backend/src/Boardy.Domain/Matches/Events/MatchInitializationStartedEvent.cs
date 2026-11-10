using Boardy.Domain.Events;

namespace Boardy.Domain.Matches.Events;

public sealed record MatchInitializationStartedEvent : DomainEvent
{
    public Guid MatchId { get; }

    public MatchInitializationStartedEvent(Guid matchId)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        MatchId = matchId;
    }
}
