using System.Collections;
using System.Reflection;
using Boardy.Domain.Events;
using Boardy.Domain.Matches;
using Boardy.Domain.Matches.Events;

namespace Boardy.Domain.Tests.Matches;

public class MatchDomainEventTests
{
    private static GameDefinitionId CreateGameDef() => GameDefinitionId.Create("catan");

    private static MatchParticipantSnapshot CreateParticipant(int seatOrder = 0, string name = "Player") =>
        MatchParticipantSnapshot.Create(Guid.NewGuid(), name, seatOrder);

    private static Match CreateTestMatch(out MatchParticipantSnapshot p1, out MatchParticipantSnapshot p2)
    {
        p1 = CreateParticipant(0, "Alice");
        p2 = CreateParticipant(1, "Bob");
        return Match.Create(CreateGameDef(), [p1, p2]);
    }

    // ==========================================
    // Event Construction & Metadata Tests
    // ==========================================

    [Fact]
    public void MatchCreatedEvent_SetsPropertiesCorrectly()
    {
        var matchId = Guid.NewGuid();
        var gameDef = CreateGameDef();
        var before = DateTimeOffset.UtcNow;

        var evt = new MatchCreatedEvent(matchId, gameDef);
        var after = DateTimeOffset.UtcNow;

        Assert.Equal(matchId, evt.MatchId);
        Assert.Equal(gameDef, evt.GameDefinitionId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
        Assert.InRange(evt.OccurredAt, before, after);
        Assert.IsAssignableFrom<DomainEvent>(evt);
    }

    [Fact]
    public void MatchCreatedEvent_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new MatchCreatedEvent(Guid.Empty, CreateGameDef()));
        Assert.Throws<ArgumentNullException>(() => new MatchCreatedEvent(Guid.NewGuid(), null!));
    }

    [Fact]
    public void MatchInitializationStartedEvent_SetsPropertiesCorrectly()
    {
        var matchId = Guid.NewGuid();
        var evt = new MatchInitializationStartedEvent(matchId);

        Assert.Equal(matchId, evt.MatchId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void MatchInitializationStartedEvent_RejectsEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => new MatchInitializationStartedEvent(Guid.Empty));
    }

    [Fact]
    public void MatchStartedEvent_SetsPropertiesCorrectly()
    {
        var matchId = Guid.NewGuid();
        var evt = new MatchStartedEvent(matchId);

        Assert.Equal(matchId, evt.MatchId);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void MatchStartedEvent_RejectsEmptyGuid()
    {
        Assert.Throws<ArgumentException>(() => new MatchStartedEvent(Guid.Empty));
    }

    [Fact]
    public void MatchCompletedEvent_SetsPropertiesCorrectly()
    {
        var matchId = Guid.NewGuid();
        var result = MatchResult.Create([Guid.NewGuid()]);
        var evt = new MatchCompletedEvent(matchId, result);

        Assert.Equal(matchId, evt.MatchId);
        Assert.Equal(result, evt.Result);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void MatchCompletedEvent_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new MatchCompletedEvent(Guid.Empty, MatchResult.Create([Guid.NewGuid()])));
        Assert.Throws<ArgumentNullException>(() => new MatchCompletedEvent(Guid.NewGuid(), null!));
    }

    [Fact]
    public void MatchAbortedEvent_SetsPropertiesCorrectly()
    {
        var matchId = Guid.NewGuid();
        var evt = new MatchAbortedEvent(matchId, MatchAbortReason.HostAborted);

        Assert.Equal(matchId, evt.MatchId);
        Assert.Equal(MatchAbortReason.HostAborted, evt.Reason);
        Assert.NotEqual(Guid.Empty, evt.EventId);
        Assert.Equal(TimeSpan.Zero, evt.OccurredAt.Offset);
    }

    [Fact]
    public void MatchAbortedEvent_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentException>(() => new MatchAbortedEvent(Guid.Empty, MatchAbortReason.HostAborted));
        Assert.Throws<ArgumentException>(() => new MatchAbortedEvent(Guid.NewGuid(), (MatchAbortReason)999));
    }

    [Fact]
    public void MatchSpectatorEvents_SetPropertiesCorrectly_AndRejectEmptyGuids()
    {
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var joined = new MatchSpectatorJoinedEvent(matchId, userId);
        Assert.Equal(matchId, joined.MatchId);
        Assert.Equal(userId, joined.UserId);
        Assert.NotEqual(Guid.Empty, joined.EventId);

        var left = new MatchSpectatorLeftEvent(matchId, userId);
        Assert.Equal(matchId, left.MatchId);
        Assert.Equal(userId, left.UserId);
        Assert.NotEqual(Guid.Empty, left.EventId);

        Assert.Throws<ArgumentException>(() => new MatchSpectatorJoinedEvent(Guid.Empty, userId));
        Assert.Throws<ArgumentException>(() => new MatchSpectatorJoinedEvent(matchId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new MatchSpectatorLeftEvent(Guid.Empty, userId));
        Assert.Throws<ArgumentException>(() => new MatchSpectatorLeftEvent(matchId, Guid.Empty));
    }

    [Fact]
    public void MatchParticipantLifecycleEvents_SetPropertiesCorrectly_AndRejectEmptyGuids()
    {
        var matchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var disconnected = new MatchParticipantDisconnectedEvent(matchId, userId);
        Assert.Equal(matchId, disconnected.MatchId);
        Assert.Equal(userId, disconnected.UserId);
        Assert.NotEqual(Guid.Empty, disconnected.EventId);

        var reconnected = new MatchParticipantReconnectedEvent(matchId, userId);
        Assert.Equal(matchId, reconnected.MatchId);
        Assert.Equal(userId, reconnected.UserId);
        Assert.NotEqual(Guid.Empty, reconnected.EventId);

        var forfeited = new MatchParticipantForfeitedEvent(matchId, userId);
        Assert.Equal(matchId, forfeited.MatchId);
        Assert.Equal(userId, forfeited.UserId);
        Assert.NotEqual(Guid.Empty, forfeited.EventId);

        Assert.Throws<ArgumentException>(() => new MatchParticipantDisconnectedEvent(Guid.Empty, userId));
        Assert.Throws<ArgumentException>(() => new MatchParticipantDisconnectedEvent(matchId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new MatchParticipantReconnectedEvent(Guid.Empty, userId));
        Assert.Throws<ArgumentException>(() => new MatchParticipantReconnectedEvent(matchId, Guid.Empty));
        Assert.Throws<ArgumentException>(() => new MatchParticipantForfeitedEvent(Guid.Empty, userId));
        Assert.Throws<ArgumentException>(() => new MatchParticipantForfeitedEvent(matchId, Guid.Empty));
    }

    [Fact]
    public void AllEvents_PropertiesDoNotExposePublicSetters()
    {
        var eventTypes = new[]
        {
            typeof(MatchCreatedEvent),
            typeof(MatchInitializationStartedEvent),
            typeof(MatchStartedEvent),
            typeof(MatchCompletedEvent),
            typeof(MatchAbortedEvent),
            typeof(MatchSpectatorJoinedEvent),
            typeof(MatchSpectatorLeftEvent),
            typeof(MatchParticipantDisconnectedEvent),
            typeof(MatchParticipantReconnectedEvent),
            typeof(MatchParticipantForfeitedEvent)
        };

        foreach (var type in eventTypes)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                Assert.True(prop.SetMethod == null || !prop.SetMethod.IsPublic,
                    $"Property '{prop.Name}' on '{type.Name}' should not have a public setter.");
            }
        }
    }

    // ==========================================
    // Aggregate Event Recording Tests
    // ==========================================

    [Fact]
    public void MatchCreate_RecordsMatchCreatedEvent()
    {
        var gameDef = CreateGameDef();
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(gameDef, [p1]);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchCreatedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(gameDef, evt.GameDefinitionId);
    }

    [Fact]
    public void BeginInitialization_RecordsMatchInitializationStartedEvent()
    {
        var match = CreateTestMatch(out _, out _);
        match.ClearDomainEvents();

        match.BeginInitialization();

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchInitializationStartedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
    }

    [Fact]
    public void Activate_RecordsMatchStartedEvent()
    {
        var match = CreateTestMatch(out _, out _);
        match.BeginInitialization();
        match.ClearDomainEvents();

        match.Activate();

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchStartedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
    }

    [Fact]
    public void Complete_RecordsMatchCompletedEvent_WithAcceptedResult()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();
        match.ClearDomainEvents();

        var result = MatchResult.Create([p1.UserId]);
        match.Complete(result);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchCompletedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(result, evt.Result);
    }

    [Fact]
    public void Abort_RecordsMatchAbortedEvent_WithAcceptedReason()
    {
        var match = CreateTestMatch(out _, out _);
        match.BeginInitialization();
        match.Activate();
        match.ClearDomainEvents();

        match.Abort(MatchAbortReason.HostAborted);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchAbortedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(MatchAbortReason.HostAborted, evt.Reason);
    }

    [Fact]
    public void AddSpectator_RecordsMatchSpectatorJoinedEvent()
    {
        var match = CreateTestMatch(out _, out _);
        match.ClearDomainEvents();
        var spectatorId = Guid.NewGuid();

        match.AddSpectator(spectatorId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchSpectatorJoinedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(spectatorId, evt.UserId);
    }

    [Fact]
    public void RemoveSpectator_RecordsMatchSpectatorLeftEvent()
    {
        var match = CreateTestMatch(out _, out _);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);
        match.ClearDomainEvents();

        match.RemoveSpectator(spectatorId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchSpectatorLeftEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(spectatorId, evt.UserId);
    }

    [Fact]
    public void DisconnectParticipant_RecordsMatchParticipantDisconnectedEvent()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();
        match.ClearDomainEvents();

        match.DisconnectParticipant(p1.UserId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchParticipantDisconnectedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(p1.UserId, evt.UserId);
    }

    [Fact]
    public void ReconnectParticipant_RecordsMatchParticipantReconnectedEvent()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);
        match.ClearDomainEvents();

        match.ReconnectParticipant(p1.UserId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchParticipantReconnectedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(p1.UserId, evt.UserId);
    }

    [Fact]
    public void ForfeitParticipant_WhenActive_RecordsMatchParticipantForfeitedEvent()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();
        match.ClearDomainEvents();

        match.ForfeitParticipant(p1.UserId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchParticipantForfeitedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(p1.UserId, evt.UserId);
    }

    [Fact]
    public void ForfeitParticipant_WhenDisconnected_RecordsMatchParticipantForfeitedEvent()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);
        match.ClearDomainEvents();

        match.ForfeitParticipant(p1.UserId);

        Assert.Single(match.DomainEvents);
        var evt = Assert.IsType<MatchParticipantForfeitedEvent>(match.DomainEvents.First());
        Assert.Equal(match.Id, evt.MatchId);
        Assert.Equal(p1.UserId, evt.UserId);
    }

    [Fact]
    public void MultipleOperations_PreserveEventChronologicalOrdering()
    {
        var match = CreateTestMatch(out var p1, out var p2);
        var spectatorId = Guid.NewGuid();

        match.AddSpectator(spectatorId);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);
        match.ReconnectParticipant(p1.UserId);
        match.ForfeitParticipant(p2.UserId);
        match.RemoveSpectator(spectatorId);
        var result = MatchResult.Create([p1.UserId]);
        match.Complete(result);

        var events = match.DomainEvents.ToList();
        Assert.Equal(9, events.Count);

        Assert.IsType<MatchCreatedEvent>(events[0]);
        Assert.IsType<MatchSpectatorJoinedEvent>(events[1]);
        Assert.IsType<MatchInitializationStartedEvent>(events[2]);
        Assert.IsType<MatchStartedEvent>(events[3]);
        Assert.IsType<MatchParticipantDisconnectedEvent>(events[4]);
        Assert.IsType<MatchParticipantReconnectedEvent>(events[5]);
        Assert.IsType<MatchParticipantForfeitedEvent>(events[6]);
        Assert.IsType<MatchSpectatorLeftEvent>(events[7]);
        Assert.IsType<MatchCompletedEvent>(events[8]);
    }

    [Fact]
    public void FailedOperations_DoNotRecordEvents_PreservingFailureAtomicity()
    {
        var match = CreateTestMatch(out var p1, out _);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);
        match.ClearDomainEvents();

        // 1. Begin initialization when already Pending with wrong state (or duplicate BeginInit)
        match.BeginInitialization();
        match.ClearDomainEvents();
        Assert.Throws<InvalidOperationException>(() => match.BeginInitialization());
        Assert.Empty(match.DomainEvents);

        // 2. Activate when not Initializing
        match.Activate();
        match.ClearDomainEvents();
        Assert.Throws<InvalidOperationException>(() => match.Activate());
        Assert.Empty(match.DomainEvents);

        // 3. Complete with invalid winner
        Assert.Throws<ArgumentException>(() => match.Complete(MatchResult.Create([Guid.NewGuid()])));
        Assert.Empty(match.DomainEvents);

        // 4. Disconnect empty guid
        Assert.Throws<ArgumentException>(() => match.DisconnectParticipant(Guid.Empty));
        Assert.Empty(match.DomainEvents);

        // 5. Disconnect unknown user
        Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(Guid.NewGuid()));
        Assert.Empty(match.DomainEvents);

        // 6. Disconnect spectator
        Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(spectatorId));
        Assert.Empty(match.DomainEvents);

        // 7. Add duplicate spectator
        Assert.Throws<InvalidOperationException>(() => match.AddSpectator(spectatorId));
        Assert.Empty(match.DomainEvents);

        // 8. Add participant as spectator
        Assert.Throws<InvalidOperationException>(() => match.AddSpectator(p1.UserId));
        Assert.Empty(match.DomainEvents);

        // 9. Remove nonexistent spectator
        Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(Guid.NewGuid()));
        Assert.Empty(match.DomainEvents);

        // State remains Active
        Assert.Equal(MatchStatus.Active, match.Status);
    }

    [Fact]
    public void ClearDomainEvents_ClearsPendingEvents_WithoutChangingMatchStateOrMembership()
    {
        var match = CreateTestMatch(out var p1, out _);
        match.BeginInitialization();
        match.Activate();

        Assert.Equal(3, match.DomainEvents.Count);

        // Act
        match.ClearDomainEvents();

        // Assert
        Assert.Empty(match.DomainEvents);
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(2, match.ParticipantMemberships.Count);
        Assert.Contains(match.ParticipantMemberships, m => m.UserId == p1.UserId);
    }

    [Fact]
    public void DomainEvents_CannotBeExternallyModified()
    {
        var match = CreateTestMatch(out _, out _);
        var dummyEvent = new MatchStartedEvent(Guid.NewGuid());

        // Generic IList
        if (match.DomainEvents is IList<IDomainEvent> list)
        {
            Assert.Throws<NotSupportedException>(() => list.Add(dummyEvent));
        }

        // Non-generic IList
        if (match.DomainEvents is IList nonGenericList)
        {
            Assert.True(nonGenericList.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => nonGenericList.Add(dummyEvent));
        }
    }

    [Fact]
    public void DomainEvents_PublicProperty_HasNoPublicSetter()
    {
        var property = typeof(Match).GetProperty(nameof(Match.DomainEvents), BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(property);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void RecordEvent_IsNotPubliclyAccessible()
    {
        var publicMethod = typeof(Match).GetMethod("RecordEvent", BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(publicMethod);

        var nonPublicMethod = typeof(Match).GetMethod("RecordEvent", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(nonPublicMethod);
        Assert.True(nonPublicMethod.IsPrivate, "RecordEvent must be strictly private on Match.");
    }
}
