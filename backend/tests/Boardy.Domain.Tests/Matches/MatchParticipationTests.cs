using System.Reflection;
using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchParticipationTests
{
    [Fact]
    public void CreateParticipant_WithValidUserId_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var participation = MatchParticipation.CreateParticipant(userId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Participant, participation.Role);
        Assert.Equal(ParticipationStatus.Active, participation.Status);
        Assert.Equal(TimeSpan.Zero, participation.JoinedAt.Offset);
        Assert.InRange(participation.JoinedAt, before, after);
    }

    [Fact]
    public void CreateParticipant_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => MatchParticipation.CreateParticipant(Guid.Empty));
        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void CreateSpectator_WithValidUserId_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        // Act
        var participation = MatchParticipation.CreateSpectator(userId);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Spectator, participation.Role);
        Assert.Null(participation.Status);
        Assert.Equal(TimeSpan.Zero, participation.JoinedAt.Offset);
        Assert.InRange(participation.JoinedAt, before, after);
    }

    [Fact]
    public void CreateSpectator_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => MatchParticipation.CreateSpectator(Guid.Empty));
        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public void Constructor_IsPrivate()
    {
        // Arrange
        var constructors = typeof(MatchParticipation).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.Empty(constructors);
    }

    [Fact]
    public void PublicProperties_DoNotExposePublicSetters()
    {
        // Arrange
        var properties = typeof(MatchParticipation).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotEmpty(properties);
        foreach (var property in properties)
        {
            Assert.True(property.SetMethod == null || !property.SetMethod.IsPublic,
                $"Property '{property.Name}' should not have a public setter.");
        }
    }

    [Fact]
    public void TransitionMethods_AreNotPublic()
    {
        // Arrange
        var transitionMethodNames = new[] { "Disconnect", "Reconnect", "Forfeit" };

        // Assert
        foreach (var methodName in transitionMethodNames)
        {
            var publicMethod = typeof(MatchParticipation).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            Assert.Null(publicMethod);

            var internalMethod = typeof(MatchParticipation).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(internalMethod);
            Assert.True(internalMethod.IsAssembly, $"Method '{methodName}' should be internal.");
        }
    }

    // ==========================================
    // Lifecycle Transition Tests
    // ==========================================

    [Fact]
    public void Disconnect_WhenActive_TransitionsToDisconnected_PreservingOtherFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var participation = MatchParticipation.CreateParticipant(userId);
        var joinedAt = participation.JoinedAt;

        // Act
        participation.Disconnect();

        // Assert
        Assert.Equal(ParticipationStatus.Disconnected, participation.Status);
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Participant, participation.Role);
        Assert.Equal(joinedAt, participation.JoinedAt);
    }

    [Fact]
    public void Reconnect_WhenDisconnected_TransitionsToActive_PreservingOtherFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var participation = MatchParticipation.CreateParticipant(userId);
        var joinedAt = participation.JoinedAt;
        participation.Disconnect();

        // Act
        participation.Reconnect();

        // Assert
        Assert.Equal(ParticipationStatus.Active, participation.Status);
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Participant, participation.Role);
        Assert.Equal(joinedAt, participation.JoinedAt);
    }

    [Fact]
    public void Forfeit_WhenActive_TransitionsToForfeited_PreservingOtherFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var participation = MatchParticipation.CreateParticipant(userId);
        var joinedAt = participation.JoinedAt;

        // Act
        participation.Forfeit();

        // Assert
        Assert.Equal(ParticipationStatus.Forfeited, participation.Status);
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Participant, participation.Role);
        Assert.Equal(joinedAt, participation.JoinedAt);
    }

    [Fact]
    public void Forfeit_WhenDisconnected_TransitionsToForfeited_PreservingOtherFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var participation = MatchParticipation.CreateParticipant(userId);
        var joinedAt = participation.JoinedAt;
        participation.Disconnect();

        // Act
        participation.Forfeit();

        // Assert
        Assert.Equal(ParticipationStatus.Forfeited, participation.Status);
        Assert.Equal(userId, participation.UserId);
        Assert.Equal(MatchRole.Participant, participation.Role);
        Assert.Equal(joinedAt, participation.JoinedAt);
    }

    [Fact]
    public void Disconnect_WhenAlreadyDisconnected_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var participation = MatchParticipation.CreateParticipant(Guid.NewGuid());
        participation.Disconnect();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => participation.Disconnect());
        Assert.Contains("already disconnected", ex.Message);
        Assert.Equal(ParticipationStatus.Disconnected, participation.Status);
    }

    [Fact]
    public void Disconnect_WhenForfeited_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var participation = MatchParticipation.CreateParticipant(Guid.NewGuid());
        participation.Forfeit();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => participation.Disconnect());
        Assert.Contains("forfeited", ex.Message);
        Assert.Equal(ParticipationStatus.Forfeited, participation.Status);
    }

    [Fact]
    public void Reconnect_WhenAlreadyActive_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var participation = MatchParticipation.CreateParticipant(Guid.NewGuid());

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => participation.Reconnect());
        Assert.Contains("already active", ex.Message);
        Assert.Equal(ParticipationStatus.Active, participation.Status);
    }

    [Fact]
    public void Reconnect_WhenForfeited_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var participation = MatchParticipation.CreateParticipant(Guid.NewGuid());
        participation.Forfeit();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => participation.Reconnect());
        Assert.Contains("forfeited", ex.Message);
        Assert.Equal(ParticipationStatus.Forfeited, participation.Status);
    }

    [Fact]
    public void Forfeit_WhenAlreadyForfeited_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var participation = MatchParticipation.CreateParticipant(Guid.NewGuid());
        participation.Forfeit();

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => participation.Forfeit());
        Assert.Contains("already forfeited", ex.Message);
        Assert.Equal(ParticipationStatus.Forfeited, participation.Status);
    }

    [Fact]
    public void Transitions_WhenSpectator_ThrowsInvalidOperationException_AndLeavesStateUnchanged()
    {
        // Arrange
        var spectator = MatchParticipation.CreateSpectator(Guid.NewGuid());

        // Act & Assert: Disconnect
        var exDisc = Assert.Throws<InvalidOperationException>(() => spectator.Disconnect());
        Assert.Contains("Spectators do not have a participation status", exDisc.Message);
        Assert.Null(spectator.Status);

        // Act & Assert: Reconnect
        var exRec = Assert.Throws<InvalidOperationException>(() => spectator.Reconnect());
        Assert.Contains("Spectators do not have a participation status", exRec.Message);
        Assert.Null(spectator.Status);

        // Act & Assert: Forfeit
        var exForf = Assert.Throws<InvalidOperationException>(() => spectator.Forfeit());
        Assert.Contains("Spectators do not have a participation status", exForf.Message);
        Assert.Null(spectator.Status);
    }

    [Fact]
    public void ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var participant = MatchParticipation.CreateParticipant(userId);
        var spectator = MatchParticipation.CreateSpectator(userId);

        // Act & Assert
        Assert.Contains(userId.ToString(), participant.ToString());
        Assert.Contains("Participant", participant.ToString());
        Assert.Contains("Active", participant.ToString());

        Assert.Contains(userId.ToString(), spectator.ToString());
        Assert.Contains("Spectator", spectator.ToString());
    }

    [Fact]
    public void PrivateConstructor_ValidatesInvariantsWhenInvoked()
    {
        var ctor = typeof(MatchParticipation).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(c => c.GetParameters().Length == 4);

        // Invalid role
        var exRole = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([Guid.NewGuid(), (MatchRole)999, ParticipationStatus.Active, DateTimeOffset.UtcNow]));
        Assert.IsType<ArgumentException>(exRole.InnerException);

        // Spectator with non-null status
        var exSpectator = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([Guid.NewGuid(), MatchRole.Spectator, ParticipationStatus.Active, DateTimeOffset.UtcNow]));
        Assert.IsType<ArgumentException>(exSpectator.InnerException);

        // Participant with null status
        var exParticipant = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([Guid.NewGuid(), MatchRole.Participant, null, DateTimeOffset.UtcNow]));
        Assert.IsType<ArgumentException>(exParticipant.InnerException);

        // Invalid participation status
        var exStatus = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([Guid.NewGuid(), MatchRole.Participant, (ParticipationStatus)999, DateTimeOffset.UtcNow]));
        Assert.IsType<ArgumentException>(exStatus.InnerException);

        // Empty user ID
        var exEmptyUser = Assert.Throws<TargetInvocationException>(() =>
            ctor.Invoke([Guid.Empty, MatchRole.Participant, ParticipationStatus.Active, DateTimeOffset.UtcNow]));
        Assert.IsType<ArgumentException>(exEmptyUser.InnerException);
    }
}
