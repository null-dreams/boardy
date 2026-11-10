using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomMatchStartedEvent : DomainEvent
{
    public Guid RoomId { get; }

    public RoomMatchStartedEvent(Guid roomId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        RoomId = roomId;
    }
}
