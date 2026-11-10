using System.Collections;
using System.Reflection;
using Boardy.Domain.Events;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;

namespace Boardy.Domain.Tests.Rooms;

public class RoomDomainEventTests
{
    private static Room CreateTestRoom(out Guid hostUserId, out RoomCode code)
    {
        hostUserId = Guid.NewGuid();
        code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;
        return Room.Create(hostUserId, code, settings);
    }

    // ==========================================
    // Event Payload & Metadata Tests
    // ==========================================

    [Fact]
    public void RoomCreatedEvent_SetsPropertiesCorrectly()
    {
        var roomId = Guid.NewGuid();
        var hostUserId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var evt = new RoomCreatedEvent(roomId, hostUserId);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(roomId, evt.RoomId);
        Assert.Equal(hostUserId, evt.HostUserId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
        Assert.InRange(evt.OccurredAt, before, after);
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void RoomCreatedEvent_RejectsEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new RoomCreatedEvent(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RoomCreatedEvent(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void RoomMemberJoinedEvent_SetsPropertiesCorrectly()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = new RoomMemberJoinedEvent(roomId, userId);

        Assert.Equal(roomId, evt.RoomId);
        Assert.Equal(userId, evt.UserId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void RoomMemberJoinedEvent_RejectsEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new RoomMemberJoinedEvent(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RoomMemberJoinedEvent(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void RoomMemberLeftEvent_SetsPropertiesCorrectly()
    {
        var roomId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evt = new RoomMemberLeftEvent(roomId, userId);

        Assert.Equal(roomId, evt.RoomId);
        Assert.Equal(userId, evt.UserId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void RoomMemberLeftEvent_RejectsEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new RoomMemberLeftEvent(Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RoomMemberLeftEvent(Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void RoomHostTransferredEvent_SetsPropertiesCorrectly()
    {
        var roomId = Guid.NewGuid();
        var previousHost = Guid.NewGuid();
        var newHost = Guid.NewGuid();
        var evt = new RoomHostTransferredEvent(roomId, previousHost, newHost);

        Assert.Equal(roomId, evt.RoomId);
        Assert.Equal(previousHost, evt.PreviousHostUserId);
        Assert.Equal(newHost, evt.NewHostUserId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void RoomHostTransferredEvent_RejectsEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new RoomHostTransferredEvent(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RoomHostTransferredEvent(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new RoomHostTransferredEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void RoomCodeRegeneratedEvent_SetsPropertiesCorrectly_WithoutExposingCode()
    {
        var roomId = Guid.NewGuid();
        var evt = new RoomCodeRegeneratedEvent(roomId);

        Assert.Equal(roomId, evt.RoomId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);

        // Verify the event record does not contain any property for the actual code string
        var properties = typeof(RoomCodeRegeneratedEvent).GetProperties();
        Assert.DoesNotContain(properties, p => p.PropertyType == typeof(RoomCode) || p.PropertyType == typeof(string));
    }

    [Fact]
    public void RoomCodeRegeneratedEvent_RejectsEmptyGuids()
    {
        Assert.Throws<ArgumentException>(() => new RoomCodeRegeneratedEvent(Guid.Empty));
    }

    [Fact]
    public void RoomMatchLifecycleEvents_SetPropertiesCorrectly_AndRejectEmptyGuids()
    {
        var roomId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        var startInit = new RoomMatchInitializationStartedEvent(roomId, matchId);
        Assert.Equal(roomId, startInit.RoomId);
        Assert.Equal(matchId, startInit.MatchId);
        Assert.NotEqual(Guid.Empty, startInit.EventId);

        var cancelInit = new RoomMatchInitializationCancelledEvent(roomId, matchId);
        Assert.Equal(roomId, cancelInit.RoomId);
        Assert.Equal(matchId, cancelInit.MatchId);
        Assert.NotEqual(Guid.Empty, cancelInit.EventId);

        var started = new RoomMatchStartedEvent(roomId, matchId);
        Assert.Equal(roomId, started.RoomId);
        Assert.Equal(matchId, started.MatchId);
        Assert.NotEqual(Guid.Empty, started.EventId);

        var ended = new RoomMatchEndedEvent(roomId, matchId);
        Assert.Equal(roomId, ended.RoomId);
        Assert.Equal(matchId, ended.MatchId);
        Assert.NotEqual(Guid.Empty, ended.EventId);

        var closed = new RoomClosedEvent(roomId);
        Assert.Equal(roomId, closed.RoomId);
        Assert.NotEqual(Guid.Empty, closed.EventId);

        Assert.Throws<ArgumentException>(() => new RoomMatchInitializationStartedEvent(Guid.Empty, matchId));
        Assert.Throws<ArgumentException>(() => new RoomMatchInitializationStartedEvent(roomId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new RoomMatchInitializationCancelledEvent(Guid.Empty, matchId));
        Assert.Throws<ArgumentException>(() => new RoomMatchInitializationCancelledEvent(roomId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new RoomMatchStartedEvent(Guid.Empty, matchId));
        Assert.Throws<ArgumentException>(() => new RoomMatchStartedEvent(roomId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new RoomMatchEndedEvent(Guid.Empty, matchId));
        Assert.Throws<ArgumentException>(() => new RoomMatchEndedEvent(roomId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new RoomClosedEvent(Guid.Empty));
    }

    // ==========================================
    // Room Aggregate Event Recording Tests
    // ==========================================

    [Fact]
    public void RoomCreate_RecordsRoomCreatedEvent()
    {
        var room = CreateTestRoom(out var hostUserId, out _);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomCreatedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(hostUserId, evt.HostUserId);
    }

    [Fact]
    public void AddMember_RecordsRoomMemberJoinedEvent()
    {
        var room = CreateTestRoom(out _, out _);
        room.ClearDomainEvents();
        var memberId = Guid.NewGuid();

        room.AddMember(memberId);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMemberJoinedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(memberId, evt.UserId);
    }

    [Fact]
    public void RemoveMember_RecordsRoomMemberLeftEvent()
    {
        var room = CreateTestRoom(out _, out _);
        var memberId = Guid.NewGuid();
        room.AddMember(memberId);
        room.ClearDomainEvents();

        room.RemoveMember(memberId);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMemberLeftEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(memberId, evt.UserId);
    }

    [Fact]
    public void TransferHost_RecordsRoomHostTransferredEvent()
    {
        var room = CreateTestRoom(out var originalHostId, out _);
        var memberId = Guid.NewGuid();
        room.AddMember(memberId);
        room.ClearDomainEvents();

        room.TransferHost(memberId);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomHostTransferredEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(originalHostId, evt.PreviousHostUserId);
        Assert.Equal(memberId, evt.NewHostUserId);
    }

    [Fact]
    public void RegenerateCode_RecordsRoomCodeRegeneratedEvent()
    {
        var room = CreateTestRoom(out _, out _);
        room.ClearDomainEvents();
        var newCode = RoomCode.Create("XYZ899");

        room.RegenerateCode(newCode);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomCodeRegeneratedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
    }

    [Fact]
    public void BeginMatch_RecordsRoomMatchInitializationStartedEvent()
    {
        var room = CreateTestRoom(out var hostId, out _);
        room.ClearDomainEvents();
        var matchId = Guid.NewGuid();

        room.BeginMatch(hostId, matchId);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMatchInitializationStartedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(matchId, evt.MatchId);
    }

    [Fact]
    public void CancelMatchInitialization_RecordsRoomMatchInitializationCancelledEvent()
    {
        var room = CreateTestRoom(out var hostId, out _);
        var matchId = Guid.NewGuid();
        room.BeginMatch(hostId, matchId);
        room.ClearDomainEvents();

        room.CancelMatchInitialization();

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMatchInitializationCancelledEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(matchId, evt.MatchId);
    }

    [Fact]
    public void MarkMatchInProgress_RecordsRoomMatchStartedEvent()
    {
        var room = CreateTestRoom(out var hostId, out _);
        var matchId = Guid.NewGuid();
        room.BeginMatch(hostId, matchId);
        room.ClearDomainEvents();

        room.MarkMatchInProgress();

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMatchStartedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(matchId, evt.MatchId);
    }

    [Fact]
    public void EndMatch_RecordsRoomMatchEndedEvent()
    {
        var room = CreateTestRoom(out var hostId, out _);
        var matchId = Guid.NewGuid();
        room.BeginMatch(hostId, matchId);
        room.MarkMatchInProgress();
        room.ClearDomainEvents();

        room.EndMatch();

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomMatchEndedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
        Assert.Equal(matchId, evt.MatchId);
    }

    [Fact]
    public void Close_RecordsRoomClosedEvent()
    {
        var room = CreateTestRoom(out var hostId, out _);
        room.ClearDomainEvents();

        room.Close(hostId);

        Assert.Single(room.DomainEvents);
        var evt = Assert.IsType<RoomClosedEvent>(room.DomainEvents.First());
        Assert.Equal(room.Id, evt.RoomId);
    }

    [Fact]
    public void MultipleOperations_RecordEventsInCorrectChronologicalOrder()
    {
        var room = CreateTestRoom(out var hostId, out _);
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        room.AddMember(member1);
        room.AddMember(member2);
        room.RegenerateCode(RoomCode.Create("NEW234"));
        room.RemoveMember(member2);
        room.TransferHost(member1);
        var matchId = Guid.NewGuid();
        room.BeginMatch(member1, matchId);
        room.MarkMatchInProgress();
        room.EndMatch();
        room.Close(member1);

        var events = room.DomainEvents.ToList();
        Assert.Equal(10, events.Count);

        Assert.IsType<RoomCreatedEvent>(events[0]);
        Assert.IsType<RoomMemberJoinedEvent>(events[1]);
        Assert.IsType<RoomMemberJoinedEvent>(events[2]);
        Assert.IsType<RoomCodeRegeneratedEvent>(events[3]);
        Assert.IsType<RoomMemberLeftEvent>(events[4]);
        Assert.IsType<RoomHostTransferredEvent>(events[5]);
        Assert.IsType<RoomMatchInitializationStartedEvent>(events[6]);
        Assert.IsType<RoomMatchStartedEvent>(events[7]);
        Assert.IsType<RoomMatchEndedEvent>(events[8]);
        Assert.IsType<RoomClosedEvent>(events[9]);
    }

    [Fact]
    public void FailedOperations_DoNotRecordEvents_PreservingFailureAtomicity()
    {
        var room = CreateTestRoom(out var hostId, out var code);
        var memberId = Guid.NewGuid();
        room.AddMember(memberId);
        room.ClearDomainEvents();

        // 1. Duplicate member
        Assert.Throws<InvalidOperationException>(() => room.AddMember(memberId));
        Assert.Empty(room.DomainEvents);

        // 2. Remove host
        Assert.Throws<InvalidOperationException>(() => room.RemoveMember(hostId));
        Assert.Empty(room.DomainEvents);

        // 3. Remove nonexistent user
        Assert.Throws<InvalidOperationException>(() => room.RemoveMember(Guid.NewGuid()));
        Assert.Empty(room.DomainEvents);

        // 4. Transfer to self
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(hostId));
        Assert.Empty(room.DomainEvents);

        // 5. Transfer to non-member
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(Guid.NewGuid()));
        Assert.Empty(room.DomainEvents);

        // 6. Regenerate identical code
        Assert.Throws<InvalidOperationException>(() => room.RegenerateCode(code));
        Assert.Empty(room.DomainEvents);

        // 7. Begin match by non-host
        Assert.Throws<InvalidOperationException>(() => room.BeginMatch(memberId, Guid.NewGuid()));
        Assert.Empty(room.DomainEvents);

        // 8. Mark match in progress when Open
        Assert.Throws<InvalidOperationException>(() => room.MarkMatchInProgress());
        Assert.Empty(room.DomainEvents);

        // 9. Cancel match init when Open
        Assert.Throws<InvalidOperationException>(() => room.CancelMatchInitialization());
        Assert.Empty(room.DomainEvents);

        // 10. End match when Open
        Assert.Throws<InvalidOperationException>(() => room.EndMatch());
        Assert.Empty(room.DomainEvents);

        // 11. Close by non-host
        Assert.Throws<InvalidOperationException>(() => room.Close(memberId));
        Assert.Empty(room.DomainEvents);

        // State remains completely intact
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Equal(hostId, room.HostUserId);
        Assert.Equal(2, room.Memberships.Count);
    }

    [Fact]
    public void ClearDomainEvents_ClearsPendingEvents_WithoutMutatingRoomState()
    {
        var room = CreateTestRoom(out var hostId, out var code);
        var memberId = Guid.NewGuid();
        room.AddMember(memberId);

        Assert.Equal(2, room.DomainEvents.Count);

        // Act
        room.ClearDomainEvents();

        // Assert
        Assert.Empty(room.DomainEvents);
        Assert.Equal(hostId, room.HostUserId);
        Assert.Equal(code, room.Code);
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Equal(2, room.Memberships.Count);
    }

    [Fact]
    public void DomainEvents_CannotBeExternallyModified()
    {
        var room = CreateTestRoom(out _, out _);
        var dummyEvent = new RoomClosedEvent(Guid.NewGuid());

        // Generic IList
        if (room.DomainEvents is IList<IDomainEvent> list)
        {
            Assert.Throws<NotSupportedException>(() => list.Add(dummyEvent));
        }

        // Non-generic IList
        if (room.DomainEvents is IList nonGenericList)
        {
            Assert.True(nonGenericList.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => nonGenericList.Add(dummyEvent));
        }
    }

    [Fact]
    public void DomainEvents_PublicProperty_HasNoPublicSetter()
    {
        var property = typeof(Room).GetProperty(nameof(Room.DomainEvents), BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void RecordEvent_IsNotPubliclyAccessible()
    {
        var publicMethod = typeof(Room).GetMethod("RecordEvent", BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(publicMethod);

        var nonPublicMethod = typeof(Room).GetMethod("RecordEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(nonPublicMethod);
        Assert.True(nonPublicMethod.IsPrivate, "RecordEvent must be strictly private to prevent external event injection.");
    }
}
