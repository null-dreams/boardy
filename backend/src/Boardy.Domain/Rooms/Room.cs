using System.Collections.ObjectModel;

namespace Boardy.Domain.Rooms;

public sealed class Room
{
    private readonly List<RoomMembership> _memberships = [];
    private readonly ReadOnlyCollection<RoomMembership> _readOnlyMemberships;

    public Guid Id { get; }
    public RoomCode Code { get; }
    public Guid HostUserId { get; private set; }
    public RoomStatus Status { get; private set; }
    public RoomSettings Settings { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<RoomMembership> Memberships => _readOnlyMemberships;

    private Room(
        Guid id,
        RoomCode code,
        Guid hostUserId,
        RoomStatus status,
        RoomSettings settings,
        DateTimeOffset createdAt,
        RoomMembership hostMembership)
    {
        Id = id;
        Code = code;
        HostUserId = hostUserId;
        Status = status;
        Settings = settings;
        CreatedAt = createdAt;
        _memberships.Add(hostMembership);
        _readOnlyMemberships = _memberships.AsReadOnly();
    }

    public static Room Create(Guid hostUserId, RoomCode code, RoomSettings settings)
    {
        if (hostUserId == Guid.Empty)
        {
            throw new ArgumentException("Host user ID cannot be empty.", nameof(hostUserId));
        }

        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(settings);

        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var hostMembership = RoomMembership.Create(hostUserId, RoomRole.Host);

        return new Room(id, code, hostUserId, RoomStatus.Open, settings, createdAt, hostMembership);
    }

    public RoomMembership AddMember(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status != RoomStatus.Open)
        {
            throw new InvalidOperationException($"Cannot add members to a room when status is {Status}.");
        }

        if (_memberships.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User '{userId}' is already a member of this room.");
        }

        if (_memberships.Count >= Settings.MaxMembers)
        {
            throw new InvalidOperationException($"Room has reached its maximum capacity of {Settings.MaxMembers} members.");
        }

        var membership = RoomMembership.Create(userId, RoomRole.Member);
        _memberships.Add(membership);
        return membership;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (userId == HostUserId)
        {
            throw new InvalidOperationException("Cannot remove the room host. Host role must be transferred before removal.");
        }

        var membership = _memberships.FirstOrDefault(m => m.UserId == userId);
        if (membership is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a member of this room.");
        }

        _memberships.Remove(membership);
    }

    public void TransferHost(Guid newHostUserId)
    {
        if (newHostUserId == Guid.Empty)
        {
            throw new ArgumentException("New host user ID cannot be empty.", nameof(newHostUserId));
        }

        if (newHostUserId == HostUserId)
        {
            throw new InvalidOperationException("Cannot transfer host role to the current host.");
        }

        var newHostMembership = _memberships.FirstOrDefault(m => m.UserId == newHostUserId);
        if (newHostMembership is null)
        {
            throw new InvalidOperationException($"User '{newHostUserId}' is not a member of this room.");
        }

        var currentHostMembership = _memberships.FirstOrDefault(m => m.UserId == HostUserId);
        if (currentHostMembership is null)
        {
            throw new InvalidOperationException("Current host membership not found in room.");
        }

        currentHostMembership.ChangeRole(RoomRole.Member);
        newHostMembership.ChangeRole(RoomRole.Host);
        HostUserId = newHostUserId;
    }
}
