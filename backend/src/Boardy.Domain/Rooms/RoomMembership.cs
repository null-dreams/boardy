using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Boardy.Domain.Tests")]

namespace Boardy.Domain.Rooms;

public sealed class RoomMembership
{
    private RoomRole _role;

    public Guid UserId { get; }
    public RoomRole Role => _role;
    public DateTimeOffset JoinedAt { get; }

    private RoomMembership(Guid userId, RoomRole role, DateTimeOffset joinedAt)
    {
        UserId = userId;
        _role = role;
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

    internal void ChangeRole(RoomRole newRole)
    {
        if (!Enum.IsDefined(newRole))
        {
            throw new ArgumentException($"Invalid room role: {newRole}.", nameof(newRole));
        }

        _role = newRole;
    }

    public override string ToString() => $"UserId: {UserId}, Role: {Role}, JoinedAt: {JoinedAt:O}";
}
