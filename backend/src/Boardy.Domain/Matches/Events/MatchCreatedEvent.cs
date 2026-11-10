using Boardy.Domain.Events;

namespace Boardy.Domain.Matches.Events;

public sealed record MatchCreatedEvent : DomainEvent
{
    public Guid MatchId { get; }
    public GameDefinitionId GameDefinitionId { get; }

    public MatchCreatedEvent(Guid matchId, GameDefinitionId gameDefinitionId)
    {
        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        ArgumentNullException.ThrowIfNull(gameDefinitionId);

        MatchId = matchId;
        GameDefinitionId = gameDefinitionId;
    }
}
