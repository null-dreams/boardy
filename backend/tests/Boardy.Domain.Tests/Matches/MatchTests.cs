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
}
