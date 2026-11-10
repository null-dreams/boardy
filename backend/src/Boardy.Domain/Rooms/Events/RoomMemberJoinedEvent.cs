using Boardy.Domain.Events;

namespace Boardy.Domain.Rooms.Events;

public sealed record RoomMemberJoinedEvent : DomainEvent
{
    public Guid RoomId { get; }
    public Guid UserId { get; }

    public RoomMemberJoinedEvent(Guid roomId, Guid userId)
    {
        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room ID cannot be empty.", nameof(roomId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        RoomId = roomId;
        UserId = userId;
    }
}
