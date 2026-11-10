using Boardy.Domain.Events;

namespace Boardy.Domain.Matches.Events;

public sealed record MatchParticipantForfeitedEvent : DomainEvent
{
    public Guid MatchId { get; }
    public Guid UserId { get; }

    public MatchParticipantForfeitedEvent(Guid matchId, Guid userId)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        MatchId = matchId;
        UserId = userId;
    }
}
