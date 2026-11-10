using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomHostTransferredEvent : DomainEvent
{
    public Guid RoomId { get; }
    public Guid PreviousHostUserId { get; }
    public Guid NewHostUserId { get; }

    public RoomHostTransferredEvent(Guid roomId, Guid previousHostUserId, Guid newHostUserId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        if (previousHostUserId == Guid.Empty)
        {
            throw new ArgumentException("Previous host user ID cannot be empty.", nameof(previousHostUserId));
        }

        if (newHostUserId == Guid.Empty)
        {
            throw new ArgumentException("New host user ID cannot be empty.", nameof(newHostUserId));
        }

        RoomId = roomId;
        PreviousHostUserId = previousHostUserId;
        NewHostUserId = newHostUserId;
    }
}
