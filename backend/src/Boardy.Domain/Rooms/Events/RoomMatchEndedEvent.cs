using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomMatchEndedEvent : DomainEvent
{
    public Guid RoomId { get; }

    public RoomMatchEndedEvent(Guid roomId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        RoomId = roomId;
    }
}
