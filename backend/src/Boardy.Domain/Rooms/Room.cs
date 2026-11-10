using System.Collections.ObjectModel;
using Boardy.Domain.Events;
using Boardy.Domain.Rooms.Events;

namespace Boardy.Domain.Rooms;

public sealed class Room
{
    private readonly List<RoomMembership> _memberships = [];
    private readonly ReadOnlyCollection<RoomMembership> _readOnlyMemberships;
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _readOnlyDomainEvents;

    public Guid Id { get; }
    public RoomCode Code { get; private set; }
    public Guid HostUserId { get; private set; }
    public RoomStatus Status { get; private set; }
    public RoomSettings Settings { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<RoomMembership> Memberships => _readOnlyMemberships;
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _readOnlyDomainEvents;

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
        _readOnlyDomainEvents = _domainEvents.AsReadOnly();
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

        var room = new Room(id, code, hostUserId, RoomStatus.Open, settings, createdAt, hostMembership);
        room.RecordEvent(new RoomCreatedEvent(id, hostUserId));
        return room;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void RecordEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
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
        RecordEvent(new RoomMemberJoinedEvent(Id, userId));
        return membership;
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status == RoomStatus.Closed)
        {
            throw new InvalidOperationException("Cannot remove members from a closed room.");
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
        RecordEvent(new RoomMemberLeftEvent(Id, userId));
    }

    public void TransferHost(Guid newHostUserId)
    {
        if (newHostUserId == Guid.Empty)
        {
            throw new ArgumentException("New host user ID cannot be empty.", nameof(newHostUserId));
        }

        if (Status == RoomStatus.Closed)
        {
            throw new InvalidOperationException("Cannot transfer host in a closed room.");
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

        var previousHostUserId = HostUserId;
        currentHostMembership.ChangeRole(RoomRole.Member);
        newHostMembership.ChangeRole(RoomRole.Host);
        HostUserId = newHostUserId;
        RecordEvent(new RoomHostTransferredEvent(Id, previousHostUserId, newHostUserId));
    }

    public void RegenerateCode(RoomCode newCode)
    {
        ArgumentNullException.ThrowIfNull(newCode);

        if (Status == RoomStatus.Closed)
        {
            throw new InvalidOperationException("Cannot regenerate room code for a closed room.");
        }

        if (newCode == Code)
        {
            throw new InvalidOperationException("New room code cannot be identical to the current room code.");
        }

        Code = newCode;
        RecordEvent(new RoomCodeRegeneratedEvent(Id));
    }

    public void BeginMatch(Guid requestedByUserId)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requester user ID cannot be empty.", nameof(requestedByUserId));
        }

        if (requestedByUserId != HostUserId)
        {
            throw new InvalidOperationException("Only the room host can initiate a match.");
        }

        if (Status != RoomStatus.Open)
        {
            throw new InvalidOperationException($"Cannot initiate a match when room status is {Status}.");
        }

        Status = RoomStatus.StartingMatch;
        RecordEvent(new RoomMatchInitializationStartedEvent(Id));
    }

    public void MarkMatchInProgress()
    {
        if (Status != RoomStatus.StartingMatch)
        {
            throw new InvalidOperationException($"Cannot mark match in progress when room status is {Status}.");
        }

        Status = RoomStatus.InMatch;
        RecordEvent(new RoomMatchStartedEvent(Id));
    }

    public void CancelMatchInitialization()
    {
        if (Status != RoomStatus.StartingMatch)
        {
            throw new InvalidOperationException($"Cannot cancel match initialization when room status is {Status}.");
        }

        Status = RoomStatus.Open;
        RecordEvent(new RoomMatchInitializationCancelledEvent(Id));
    }

    public void EndMatch()
    {
        if (Status != RoomStatus.InMatch)
        {
            throw new InvalidOperationException($"Cannot end match when room status is {Status}.");
        }

        Status = RoomStatus.Open;
        RecordEvent(new RoomMatchEndedEvent(Id));
    }

    public void Close(Guid requestedByUserId)
    {
        if (requestedByUserId == Guid.Empty)
        {
            throw new ArgumentException("Requester user ID cannot be empty.", nameof(requestedByUserId));
        }

        if (requestedByUserId != HostUserId)
        {
            throw new InvalidOperationException("Only the room host can close the room.");
        }

        if (Status != RoomStatus.Open)
        {
            throw new InvalidOperationException($"Cannot close a room when status is {Status}.");
        }

        Status = RoomStatus.Closed;
        RecordEvent(new RoomClosedEvent(Id));
    }
}
