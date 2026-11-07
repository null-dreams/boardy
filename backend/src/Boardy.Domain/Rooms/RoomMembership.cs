namespace Boardy.Domain.Rooms;

public sealed class RoomMembership
{
    public Guid UserId { get; }
    public RoomRole Role { get; }
    public DateTimeOffset JoinedAt { get; }

    private RoomMembership(Guid userId, RoomRole role, DateTimeOffset joinedAt)
    {
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public static RoomMembership Create(Guid userId, RoomRole role)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException($"Invalid room role: {role}.", nameof(role));
        }

        return new RoomMembership(userId, role, DateTimeOffset.UtcNow);
    }

    public override string ToString() => $"UserId: {UserId}, Role: {Role}, JoinedAt: {JoinedAt:O}";
}

