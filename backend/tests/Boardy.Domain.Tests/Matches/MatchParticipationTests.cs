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
