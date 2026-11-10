using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomMatchInitializationCancelledEvent : DomainEvent
{
    public Guid RoomId { get; }

    public RoomMatchInitializationCancelledEvent(Guid roomId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        RoomId = roomId;
    }
}
