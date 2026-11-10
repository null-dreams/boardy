using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomMatchInitializationStartedEvent : DomainEvent
{
    public Guid RoomId { get; }
    public Guid MatchId { get; }

    public RoomMatchInitializationStartedEvent(Guid roomId, Guid matchId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        if (matchId == Guid.Empty)
        {
            throw new ArgumentException("Match ID cannot be empty.", nameof(matchId));
        }

        RoomId = roomId;
        MatchId = matchId;
    }
}
