using Boardy.Domain.Events;

namespace Boardy.Domain.Matches.Events;

public sealed record MatchAbortedEvent : DomainEvent
{
    public Guid MatchId { get; }
    public MatchAbortReason Reason { get; }

    public MatchAbortedEvent(Guid matchId, MatchAbortReason reason)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentException($"Invalid match abort reason '{reason}'.", nameof(reason));
        }

        MatchId = matchId;
        Reason = reason;
    }
}
