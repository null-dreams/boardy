using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomCreatedEvent : DomainEvent
{
    public Guid RoomId { get; }
    public Guid HostUserId { get; }

    public RoomCreatedEvent(Guid roomId, Guid hostUserId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        if (hostUserId == Guid.Empty)
        {
            throw new ArgumentException("Host user ID cannot be empty.", nameof(hostUserId));
        }

        RoomId = roomId;
        HostUserId = hostUserId;
    }
}
