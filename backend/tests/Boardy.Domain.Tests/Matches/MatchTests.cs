using System.Collections;
using System.Reflection;
using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchTests
{
    private static GameDefinitionId CreateGameDef() => GameDefinitionId.Create("catan");

    private static MatchParticipantSnapshot CreateParticipant(int seatOrder = 0, string name = "Player") =>
        MatchParticipantSnapshot.Create(Guid.NewGuid(), name, seatOrder);

    // ==========================================
    // Creation Tests
    // ==========================================

    [Fact]
    public void Create_WithValidParameters_SuccessfullyCreatesMatch()
    {
        // Arrange
        var gameDef = CreateGameDef();
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var before = DateTimeOffset.UtcNow;

        // Act
        var match = Match.Create(gameDef, [p1, p2]);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotEqual(Guid.Empty, match.Id);
        Assert.Equal(gameDef, match.GameDefinitionId);
        Assert.Equal(MatchStatus.Pending, match.Status);
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(TimeSpan.Zero, match.CreatedAt.Offset);
        Assert.InRange(match.CreatedAt, before, after);

        Assert.Null(match.StartedAt);
        Assert.Null(match.CompletedAt);
        Assert.Null(match.AbortedAt);
        Assert.Null(match.Result);
        Assert.Null(match.AbortReason);
    }

    [Fact]
    public void Create_GeneratesUniqueNonEmptyIds()
    {
        // Arrange
        var gameDef = CreateGameDef();
        var p1 = CreateParticipant();

        // Act
        var match1 = Match.Create(gameDef, [p1]);
        var match2 = Match.Create(gameDef, [p1]);

        // Assert
        Assert.NotEqual(Guid.Empty, match1.Id);
        Assert.NotEqual(Guid.Empty, match2.Id);
        Assert.NotEqual(match1.Id, match2.Id);
    }

    [Fact]
    public void Create_WithNullGameDefinition_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Match.Create(null!, [CreateParticipant()]));
    }

    [Fact]
    public void Create_WithNullParticipants_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Match.Create(CreateGameDef(), null!));
    }

    [Fact]
    public void Create_WithEmptyParticipants_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Match.Create(CreateGameDef(), Array.Empty<MatchParticipantSnapshot>()));
    }

    [Fact]
    public void Create_WithDuplicateParticipantUserIds_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var p1 = MatchParticipantSnapshot.Create(userId, "Alice", 0);
        var p2 = MatchParticipantSnapshot.Create(userId, "Bob", 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Match.Create(CreateGameDef(), [p1, p2]));
    }

    [Fact]
    public void Create_WithDuplicateSeatOrders_ThrowsArgumentException()
    {
        // Arrange
        var p1 = MatchParticipantSnapshot.Create(Guid.NewGuid(), "Alice", 0);
        var p2 = MatchParticipantSnapshot.Create(Guid.NewGuid(), "Bob", 0);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Match.Create(CreateGameDef(), [p1, p2]));
    }

    [Fact]
    public void Create_MaterializesParticipants_AndPreservesOrdering()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var inputList = new List<MatchParticipantSnapshot> { p1, p2 };

        // Act
        var match = Match.Create(CreateGameDef(), inputList);
        inputList.Add(CreateParticipant(2, "Charlie"));

        // Assert
        Assert.Equal(2, match.Participants.Count);
        var participants = match.Participants.ToList();
        Assert.Equal(p1, participants[0]);
        Assert.Equal(p2, participants[1]);
    }

    [Fact]
    public void Participants_CannotBeExternallyModified()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant(0)]);

        // Act & Assert
        if (match.Participants is IList<MatchParticipantSnapshot> list)
        {
            Assert.Throws<NotSupportedException>(() => list.Add(CreateParticipant(1)));
        }
    }

    // ==========================================
    // Initialization and Activation Tests
    // ==========================================

    [Fact]
    public void BeginInitialization_FromPending_TransitionsToInitializing()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);

        // Act
        match.BeginInitialization();

        // Assert
        Assert.Equal(MatchStatus.Initializing, match.Status);
        Assert.Null(match.StartedAt);
    }

    [Fact]
    public void BeginInitialization_WhenNotInPendingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();

        // Act & Assert (from Initializing)
        Assert.Throws<InvalidOperationException>(() => match.BeginInitialization());

        // From Active
        match.Activate();
        Assert.Throws<InvalidOperationException>(() => match.BeginInitialization());
    }

    [Fact]
    public void Activate_FromInitializing_TransitionsToActiveAndSetsStartedAt()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();
        var before = DateTimeOffset.UtcNow;

        // Act
        match.Activate();
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.NotNull(match.StartedAt);
        Assert.Equal(TimeSpan.Zero, match.StartedAt.Value.Offset);
        Assert.InRange(match.StartedAt.Value, before, after);
    }

    [Fact]
    public void Activate_WhenNotInInitializingStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);

        // From Pending
        Assert.Throws<InvalidOperationException>(() => match.Activate());

        // From Active (repeated activation)
        match.BeginInitialization();
        match.Activate();
        Assert.Throws<InvalidOperationException>(() => match.Activate());
    }

    // ==========================================
    // Completion Tests
    // ==========================================

    [Fact]
    public void Complete_WithSingleWinner_TransitionsToCompletedAndSetsResult()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);
        match.BeginInitialization();
        match.Activate();

        var result = MatchResult.Create([p1.UserId]);
        var before = DateTimeOffset.UtcNow;

        // Act
        match.Complete(result);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(result, match.Result);
        Assert.NotNull(match.CompletedAt);
        Assert.Equal(TimeSpan.Zero, match.CompletedAt.Value.Offset);
        Assert.InRange(match.CompletedAt.Value, before, after);
        Assert.Null(match.AbortedAt);
        Assert.Null(match.AbortReason);
    }

    [Fact]
    public void Complete_WithMultipleWinners_SuccessfullyCompletes()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);
        match.BeginInitialization();
        match.Activate();

        var result = MatchResult.Create([p1.UserId, p2.UserId]);

        // Act
        match.Complete(result);

        // Assert
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(result, match.Result);
    }

    [Fact]
    public void Complete_WithDraw_SuccessfullyCompletes()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();

        var drawResult = MatchResult.Create(Array.Empty<Guid>());

        // Act
        match.Complete(drawResult);

        // Assert
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.True(match.Result!.IsDraw);
    }

    [Fact]
    public void Complete_WithNullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();
        match.Activate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => match.Complete(null!));
    }

    [Fact]
    public void Complete_WithNonParticipantWinner_ThrowsArgumentException()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();

        var nonParticipantWinner = MatchResult.Create([Guid.NewGuid()]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => match.Complete(nonParticipantWinner));
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Null(match.CompletedAt);
        Assert.Null(match.Result);
    }

    [Fact]
    public void Complete_WhenNotInActiveStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var result = MatchResult.Create([p1.UserId]);

        // From Pending
        Assert.Throws<InvalidOperationException>(() => match.Complete(result));

        // From Initializing
        match.BeginInitialization();
        Assert.Throws<InvalidOperationException>(() => match.Complete(result));
    }

    // ==========================================
    // Abortion Tests
    // ==========================================

    [Fact]
    public void Abort_FromInitializing_WithInitializationFailed_TransitionsToAborted()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();
        var before = DateTimeOffset.UtcNow;

        // Act
        match.Abort(MatchAbortReason.InitializationFailed);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(MatchStatus.Aborted, match.Status);
        Assert.Equal(MatchAbortReason.InitializationFailed, match.AbortReason);
        Assert.NotNull(match.AbortedAt);
        Assert.Equal(TimeSpan.Zero, match.AbortedAt.Value.Offset);
        Assert.InRange(match.AbortedAt.Value, before, after);

        Assert.Null(match.StartedAt);
        Assert.Null(match.CompletedAt);
        Assert.Null(match.Result);
    }

    [Theory]
    [InlineData(MatchAbortReason.HostAborted)]
    [InlineData(MatchAbortReason.AllPlayersDisconnected)]
    [InlineData(MatchAbortReason.AdministrativeTermination)]
    public void Abort_FromActive_WithActiveReasons_TransitionsToAborted(MatchAbortReason activeReason)
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();
        match.Activate();
        var startedAt = match.StartedAt;
        var before = DateTimeOffset.UtcNow;

        // Act
        match.Abort(activeReason);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(MatchStatus.Aborted, match.Status);
        Assert.Equal(activeReason, match.AbortReason);
        Assert.Equal(startedAt, match.StartedAt);
        Assert.NotNull(match.AbortedAt);
        Assert.InRange(match.AbortedAt.Value, before, after);
        Assert.Null(match.CompletedAt);
        Assert.Null(match.Result);
    }

    [Fact]
    public void Abort_WithUndefinedReason_ThrowsArgumentException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => match.Abort((MatchAbortReason)999));
    }

    [Theory]
    [InlineData(MatchAbortReason.HostAborted)]
    [InlineData(MatchAbortReason.AllPlayersDisconnected)]
    [InlineData(MatchAbortReason.AdministrativeTermination)]
    public void Abort_FromInitializing_WithActiveReasons_ThrowsInvalidOperationException(MatchAbortReason activeReason)
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match.Abort(activeReason));
        Assert.Equal(MatchStatus.Initializing, match.Status);
    }

    [Fact]
    public void Abort_FromActive_WithInitializationFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);
        match.BeginInitialization();
        match.Activate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match.Abort(MatchAbortReason.InitializationFailed));
        Assert.Equal(MatchStatus.Active, match.Status);
    }

    [Fact]
    public void Abort_FromPending_ThrowsInvalidOperationException()
    {
        // Arrange
        var match = Match.Create(CreateGameDef(), [CreateParticipant()]);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match.Abort(MatchAbortReason.InitializationFailed));
        Assert.Throws<InvalidOperationException>(() => match.Abort(MatchAbortReason.HostAborted));
        Assert.Equal(MatchStatus.Pending, match.Status);
    }

    // ==========================================
    // Terminal State and Integrity Tests
    // ==========================================

    [Fact]
    public void CompletedMatch_CannotBeAborted_OrTransitionFurther()
    {
        // Arrange
        var p1 = CreateParticipant();
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.Complete(MatchResult.Create([p1.UserId]));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match.Abort(MatchAbortReason.HostAborted));
        Assert.Throws<InvalidOperationException>(() => match.BeginInitialization());
        Assert.Throws<InvalidOperationException>(() => match.Activate());
        Assert.Throws<InvalidOperationException>(() => match.Complete(MatchResult.Create([p1.UserId])));

        Assert.Equal(MatchStatus.Completed, match.Status);
    }

    [Fact]
    public void AbortedMatch_CannotBeCompleted_OrTransitionFurther()
    {
        // Arrange
        var p1 = CreateParticipant();
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.Abort(MatchAbortReason.HostAborted);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => match.Complete(MatchResult.Create([p1.UserId])));
        Assert.Throws<InvalidOperationException>(() => match.BeginInitialization());
        Assert.Throws<InvalidOperationException>(() => match.Activate());
        Assert.Throws<InvalidOperationException>(() => match.Abort(MatchAbortReason.AdministrativeTermination));

        Assert.Equal(MatchStatus.Aborted, match.Status);
    }

    [Fact]
    public void ParticipantSnapshots_AndIdentity_RemainPreservedThroughAllTransitions()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var gameDef = CreateGameDef();
        var match = Match.Create(gameDef, [p1, p2]);

        var originalId = match.Id;
        var originalCreatedAt = match.CreatedAt;

        // Pending -> Initializing
        match.BeginInitialization();
        Assert.Equal(originalId, match.Id);
        Assert.Equal(gameDef, match.GameDefinitionId);
        Assert.Equal(originalCreatedAt, match.CreatedAt);
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(p1, match.Participants.First());

        // Initializing -> Active
        match.Activate();
        Assert.Equal(originalId, match.Id);
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(p1, match.Participants.First());

        // Active -> Completed
        match.Complete(MatchResult.Create([p1.UserId]));
        Assert.Equal(originalId, match.Id);
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(p1, match.Participants.First());
    }

    [Fact]
    public void PublicProperties_DoNotExposePublicSetters()
    {
        // Arrange
        var properties = typeof(Match).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotEmpty(properties);
        foreach (var property in properties)
        {
            Assert.True(property.SetMethod == null || !property.SetMethod.IsPublic,
                $"Property '{property.Name}' should not have a public setter.");
        }
    }

    // ==========================================
    // Membership Management Tests
    // ==========================================

    [Fact]
    public void Create_InitializesParticipantMembershipsFromSnapshots()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");

        // Act
        var match = Match.Create(CreateGameDef(), [p1, p2]);

        // Assert
        Assert.Equal(2, match.ParticipantMemberships.Count);
        Assert.Empty(match.SpectatorMemberships);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(MatchRole.Participant, m1.Role);
        Assert.Equal(ParticipationStatus.Active, m1.Status);

        var m2 = match.ParticipantMemberships.Single(m => m.UserId == p2.UserId);
        Assert.Equal(MatchRole.Participant, m2.Role);
        Assert.Equal(ParticipationStatus.Active, m2.Status);
    }

    [Fact]
    public void AddSpectator_WithValidUserId_AdmitsSpectatorSuccessfully()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorUserId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var spectator = match.AddSpectator(spectatorUserId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(spectator);
        Assert.Equal(spectatorUserId, spectator.UserId);
        Assert.Equal(MatchRole.Spectator, spectator.Role);
        Assert.Null(spectator.Status);
        Assert.InRange(spectator.JoinedAt, before, after);

        Assert.Single(match.SpectatorMemberships);
        Assert.Same(spectator, match.SpectatorMemberships.First());

        // Participants unchanged
        Assert.Single(match.Participants);
        Assert.Single(match.ParticipantMemberships);
    }

    [Fact]
    public void AddSpectator_WithEmptyUserId_ThrowsArgumentException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => match.AddSpectator(Guid.Empty));
        Assert.Equal("userId", ex.ParamName);
        Assert.Empty(match.SpectatorMemberships);
    }

    [Fact]
    public void AddSpectator_WithParticipantUserId_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.AddSpectator(p1.UserId));
        Assert.Contains(p1.UserId.ToString(), ex.Message);
        Assert.Empty(match.SpectatorMemberships);
    }

    [Fact]
    public void AddSpectator_WithAlreadyAdmittedSpectator_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorUserId = Guid.NewGuid();
        match.AddSpectator(spectatorUserId);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.AddSpectator(spectatorUserId));
        Assert.Contains(spectatorUserId.ToString(), ex.Message);
        Assert.Single(match.SpectatorMemberships);
    }

    [Fact]
    public void ParticipantMemberships_AreNotRemovable_AndExposeNoRemovalMethod()
    {
        // Assert: Match type must not have RemoveParticipant method
        var removeMethod = typeof(Match).GetMethod("RemoveParticipant", BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(removeMethod);
    }

    [Fact]
    public void ParticipantMemberships_RemainPresentAndUnchanged_AcrossSpectatorMutations()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);

        var spectatorId = Guid.NewGuid();

        // Act: Add and remove spectator
        match.AddSpectator(spectatorId);
        match.RemoveSpectator(spectatorId);

        // Assert: Participant memberships must remain exactly 2 with matching user IDs
        Assert.Equal(2, match.ParticipantMemberships.Count);
        Assert.Contains(match.ParticipantMemberships, m => m.UserId == p1.UserId);
        Assert.Contains(match.ParticipantMemberships, m => m.UserId == p2.UserId);

        // Snapshots unchanged
        Assert.Equal(2, match.Participants.Count);
        Assert.Contains(p1, match.Participants);
        Assert.Contains(p2, match.Participants);
    }

    [Fact]
    public void ParticipantMemberships_RemainPresentAndUnchanged_ThroughoutMatchLifecycle()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);

        // Pending
        Assert.Equal(2, match.ParticipantMemberships.Count);

        // Initializing
        match.BeginInitialization();
        Assert.Equal(2, match.ParticipantMemberships.Count);

        // Active
        match.Activate();
        Assert.Equal(2, match.ParticipantMemberships.Count);

        // Completed
        match.Complete(MatchResult.Create([p1.UserId]));
        Assert.Equal(2, match.ParticipantMemberships.Count);
        Assert.Contains(match.ParticipantMemberships, m => m.UserId == p1.UserId && m.Role == MatchRole.Participant);
        Assert.Contains(match.ParticipantMemberships, m => m.UserId == p2.UserId && m.Role == MatchRole.Participant);
    }

    [Fact]
    public void RemoveSpectator_WithValidUserId_RemovesFromSpectatorMemberships_PreservingParticipants()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        match.AddSpectator(s1);
        match.AddSpectator(s2);

        // Act
        match.RemoveSpectator(s1);

        // Assert
        Assert.Single(match.SpectatorMemberships);
        Assert.Equal(s2, match.SpectatorMemberships.Single().UserId);

        // Participants unaffected
        Assert.Single(match.ParticipantMemberships);
        Assert.Single(match.Participants);
    }

    [Fact]
    public void RemoveSpectator_WithEmptyUserId_ThrowsArgumentException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => match.RemoveSpectator(Guid.Empty));
        Assert.Equal("userId", ex.ParamName);
        Assert.Single(match.SpectatorMemberships);
    }

    [Fact]
    public void RemoveSpectator_WithNonexistentUserId_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var unknownId = Guid.NewGuid();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(unknownId));
        Assert.Contains(unknownId.ToString(), ex.Message);
    }

    [Fact]
    public void RemoveSpectator_WithParticipantUserId_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(p1.UserId));
        Assert.Contains(p1.UserId.ToString(), ex.Message);
    }

    [Fact]
    public void RemoveSpectator_CalledTwiceOnSameUser_ThrowsInvalidOperationExceptionOnSecondCall()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);

        // First removal succeeds
        match.RemoveSpectator(spectatorId);
        Assert.Empty(match.SpectatorMemberships);

        // Second removal fails
        var ex = Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(spectatorId));
        Assert.Contains(spectatorId.ToString(), ex.Message);
        Assert.Empty(match.SpectatorMemberships);
    }

    [Theory]
    [InlineData(MatchStatus.Pending)]
    [InlineData(MatchStatus.Initializing)]
    [InlineData(MatchStatus.Active)]
    public void SpectatorOperations_AllowedInNonTerminalStates(MatchStatus targetStatus)
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);

        if (targetStatus >= MatchStatus.Initializing)
        {
            match.BeginInitialization();
        }

        if (targetStatus >= MatchStatus.Active)
        {
            match.Activate();
        }

        // Act & Assert: Add spectator
        var spectatorId = Guid.NewGuid();
        var spectator = match.AddSpectator(spectatorId);
        Assert.Equal(spectatorId, spectator.UserId);
        Assert.Single(match.SpectatorMemberships);

        // Act & Assert: Remove spectator
        match.RemoveSpectator(spectatorId);
        Assert.Empty(match.SpectatorMemberships);

        // Participant memberships remain unchanged
        Assert.Equal(2, match.ParticipantMemberships.Count);
    }

    [Fact]
    public void SpectatorOperations_RejectedWhenCompleted_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);

        match.BeginInitialization();
        match.Activate();
        match.Complete(MatchResult.Create([p1.UserId]));

        var newSpectatorId = Guid.NewGuid();

        // Act & Assert: AddSpectator
        var exAdd = Assert.Throws<InvalidOperationException>(() => match.AddSpectator(newSpectatorId));
        Assert.Contains("Completed", exAdd.Message);

        // Act & Assert: RemoveSpectator
        var exRemSpec = Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(spectatorId));
        Assert.Contains("Completed", exRemSpec.Message);

        // Verify state remains unchanged
        Assert.Single(match.ParticipantMemberships);
        Assert.Single(match.SpectatorMemberships);
        Assert.Single(match.Participants);
        Assert.Equal(MatchStatus.Completed, match.Status);
    }

    [Fact]
    public void SpectatorOperations_RejectedWhenAborted_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);

        match.BeginInitialization();
        match.Abort(MatchAbortReason.InitializationFailed);

        var newSpectatorId = Guid.NewGuid();

        // Act & Assert: AddSpectator
        var exAdd = Assert.Throws<InvalidOperationException>(() => match.AddSpectator(newSpectatorId));
        Assert.Contains("Aborted", exAdd.Message);

        // Act & Assert: RemoveSpectator
        var exRemSpec = Assert.Throws<InvalidOperationException>(() => match.RemoveSpectator(spectatorId));
        Assert.Contains("Aborted", exRemSpec.Message);

        // Verify state remains unchanged
        Assert.Single(match.ParticipantMemberships);
        Assert.Single(match.SpectatorMemberships);
        Assert.Single(match.Participants);
        Assert.Equal(MatchStatus.Aborted, match.Status);
    }

    [Fact]
    public void SpectatorsProperty_DoesNotExistOnMatch()
    {
        // Assert
        var property = typeof(Match).GetProperty("Spectators", BindingFlags.Public | BindingFlags.Instance);
        Assert.Null(property);
    }

    [Fact]
    public void MembershipCollections_CannotBeExternallyModified()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);

        var dummyParticipation = MatchParticipation.CreateSpectator(Guid.NewGuid());

        // ParticipantMemberships generic IList
        if (match.ParticipantMemberships is IList<MatchParticipation> participantList)
        {
            Assert.Throws<NotSupportedException>(() => participantList.Add(dummyParticipation));
        }

        // ParticipantMemberships non-generic IList
        if (match.ParticipantMemberships is IList participantNonGeneric)
        {
            Assert.True(participantNonGeneric.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => participantNonGeneric.Add(dummyParticipation));
        }

        // SpectatorMemberships generic IList
        if (match.SpectatorMemberships is IList<MatchParticipation> spectatorMembershipsList)
        {
            Assert.Throws<NotSupportedException>(() => spectatorMembershipsList.Add(dummyParticipation));
        }

        // SpectatorMemberships non-generic IList
        if (match.SpectatorMemberships is IList spectatorMembershipsNonGeneric)
        {
            Assert.True(spectatorMembershipsNonGeneric.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => spectatorMembershipsNonGeneric.Add(dummyParticipation));
        }
    }

    // ==========================================
    // Participation Lifecycle & Forfeiture Tests
    // ==========================================

    [Fact]
    public void DisconnectParticipant_WhenMatchActiveAndParticipantActive_TransitionsToDisconnected()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);
        match.BeginInitialization();
        match.Activate();

        // Act
        match.DisconnectParticipant(p1.UserId);

        // Assert
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Disconnected, m1.Status);

        var m2 = match.ParticipantMemberships.Single(m => m.UserId == p2.UserId);
        Assert.Equal(ParticipationStatus.Active, m2.Status);

        // Snapshots unchanged
        Assert.Equal(2, match.Participants.Count);
        Assert.Equal(2, match.ParticipantMemberships.Count);
    }

    [Fact]
    public void ReconnectParticipant_WhenMatchActiveAndParticipantDisconnected_TransitionsToActive()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);

        // Act
        match.ReconnectParticipant(p1.UserId);

        // Assert
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ForfeitParticipant_WhenMatchActiveAndParticipantActive_TransitionsToForfeited()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();

        // Act
        match.ForfeitParticipant(p1.UserId);

        // Assert
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Forfeited, m1.Status);
    }

    [Fact]
    public void ForfeitParticipant_WhenMatchActiveAndParticipantDisconnected_TransitionsToForfeited()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);

        // Act
        match.ForfeitParticipant(p1.UserId);

        // Assert
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Forfeited, m1.Status);
    }

    [Fact]
    public void DisconnectParticipant_WhenAlreadyDisconnected_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.DisconnectParticipant(p1.UserId);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(p1.UserId));
        Assert.Contains("already disconnected", ex.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Disconnected, m1.Status);
    }

    [Fact]
    public void ReconnectParticipant_WhenAlreadyActive_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("already active", ex.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ReconnectParticipant_WhenForfeited_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.ForfeitParticipant(p1.UserId);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("forfeited", ex.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Forfeited, m1.Status);
    }

    [Fact]
    public void ForfeitParticipant_WhenAlreadyForfeited_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.ForfeitParticipant(p1.UserId);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(p1.UserId));
        Assert.Contains("already forfeited", ex.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Forfeited, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_WithEmptyUserId_ThrowsArgumentException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<ArgumentException>(() => match.DisconnectParticipant(Guid.Empty));
        Assert.Equal("userId", exDisc.ParamName);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<ArgumentException>(() => match.ReconnectParticipant(Guid.Empty));
        Assert.Equal("userId", exRec.ParamName);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<ArgumentException>(() => match.ForfeitParticipant(Guid.Empty));
        Assert.Equal("userId", exForf.ParamName);

        // State remains active
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_WithUnknownUserId_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        var unknownUserId = Guid.NewGuid();

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(unknownUserId));
        Assert.Contains(unknownUserId.ToString(), exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(unknownUserId));
        Assert.Contains(unknownUserId.ToString(), exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(unknownUserId));
        Assert.Contains(unknownUserId.ToString(), exForf.Message);

        // State remains active
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_WithSpectatorUserId_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        var spectatorId = Guid.NewGuid();
        match.AddSpectator(spectatorId);
        match.BeginInitialization();
        match.Activate();

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(spectatorId));
        Assert.Contains(spectatorId.ToString(), exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(spectatorId));
        Assert.Contains(spectatorId.ToString(), exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(spectatorId));
        Assert.Contains(spectatorId.ToString(), exForf.Message);

        // Participant status remains active, spectator status remains null
        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
        var s1 = match.SpectatorMemberships.Single(s => s.UserId == spectatorId);
        Assert.Null(s1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_RejectedWhenPending_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(p1.UserId));
        Assert.Contains("Pending", exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("Pending", exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(p1.UserId));
        Assert.Contains("Pending", exForf.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_RejectedWhenInitializing_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(p1.UserId));
        Assert.Contains("Initializing", exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("Initializing", exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(p1.UserId));
        Assert.Contains("Initializing", exForf.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_RejectedWhenCompleted_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Activate();
        match.Complete(MatchResult.Create([p1.UserId]));

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(p1.UserId));
        Assert.Contains("Completed", exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("Completed", exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(p1.UserId));
        Assert.Contains("Completed", exForf.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_RejectedWhenAborted_AndLeavesStateUnchanged()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var match = Match.Create(CreateGameDef(), [p1]);
        match.BeginInitialization();
        match.Abort(MatchAbortReason.InitializationFailed);

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => match.DisconnectParticipant(p1.UserId));
        Assert.Contains("Aborted", exDisc.Message);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => match.ReconnectParticipant(p1.UserId));
        Assert.Contains("Aborted", exRec.Message);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => match.ForfeitParticipant(p1.UserId));
        Assert.Contains("Aborted", exForf.Message);

        var m1 = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Equal(ParticipationStatus.Active, m1.Status);
    }

    [Fact]
    public void ParticipantLifecycleOperations_PreserveSnapshotsAndMembershipIntegrity()
    {
        // Arrange
        var p1 = CreateParticipant(0, "Alice");
        var p2 = CreateParticipant(1, "Bob");
        var match = Match.Create(CreateGameDef(), [p1, p2]);
        match.BeginInitialization();
        match.Activate();

        var m1Before = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        var originalJoinedAt = m1Before.JoinedAt;

        // Act
        match.DisconnectParticipant(p1.UserId);
        match.ReconnectParticipant(p1.UserId);
        match.ForfeitParticipant(p1.UserId);

        // Assert
        var m1After = match.ParticipantMemberships.Single(m => m.UserId == p1.UserId);
        Assert.Same(m1Before, m1After);
        Assert.Equal(p1.UserId, m1After.UserId);
        Assert.Equal(MatchRole.Participant, m1After.Role);
        Assert.Equal(originalJoinedAt, m1After.JoinedAt);
        Assert.Equal(ParticipationStatus.Forfeited, m1After.Status);

        // Snapshots unchanged
        Assert.Equal(2, match.Participants.Count);
        Assert.Contains(p1, match.Participants);
        Assert.Contains(p2, match.Participants);
    }
}
