using System.Reflection;
using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchParticipantSnapshotTests
{
    [Fact]
    public void Create_WithValidParameters_SuccessfullyCreatesSnapshot()
    {
        // Arrange
        var userId = Guid.NewGuid();
        const string displayName = "Alice";
        const int seatOrder = 0;

        // Act
        var snapshot = MatchParticipantSnapshot.Create(userId, displayName, seatOrder);

        // Assert
        Assert.Equal(userId, snapshot.UserId);
        Assert.Equal("Alice", snapshot.DisplayName);
        Assert.Equal(0, snapshot.SeatOrder);
    }

    [Fact]
    public void Create_PreservesUserId()
    {
        // Arrange
        var expectedUserId = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        var snapshot = MatchParticipantSnapshot.Create(expectedUserId, "PlayerOne", 1);

        // Assert
        Assert.Equal(expectedUserId, snapshot.UserId);
    }

    [Theory]
    [InlineData("  Bob  ", "Bob")]
    [InlineData("\tCharlie\n", "Charlie")]
    [InlineData("Dave", "Dave")]
    public void Create_TrimsWhitespaceFromDisplayName(string rawName, string expectedTrimmedName)
    {
        // Act
        var snapshot = MatchParticipantSnapshot.Create(Guid.NewGuid(), rawName, 2);

        // Assert
        Assert.Equal(expectedTrimmedName, snapshot.DisplayName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Create_WithZeroAndPositiveSeatOrder_Succeeds(int seatOrder)
    {
        // Act
        var snapshot = MatchParticipantSnapshot.Create(Guid.NewGuid(), "Player", seatOrder);

        // Assert
        Assert.Equal(seatOrder, snapshot.SeatOrder);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => MatchParticipantSnapshot.Create(Guid.Empty, "Player", 0));
    }

    [Fact]
    public void Create_WithNullDisplayName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MatchParticipantSnapshot.Create(Guid.NewGuid(), null!, 0));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Create_WithEmptyOrWhitespaceDisplayName_ThrowsArgumentException(string invalidDisplayName)
    {
        Assert.Throws<ArgumentException>(() => MatchParticipantSnapshot.Create(Guid.NewGuid(), invalidDisplayName, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-5)]
    [InlineData(-100)]
    public void Create_WithNegativeSeatOrder_ThrowsArgumentOutOfRangeException(int negativeSeatOrder)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MatchParticipantSnapshot.Create(Guid.NewGuid(), "Player", negativeSeatOrder));
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnTrueForIdenticalProperties()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var snapshot1 = MatchParticipantSnapshot.Create(userId, "Player1", 0);
        var snapshot2 = MatchParticipantSnapshot.Create(userId, "Player1", 0);

        // Assert
        Assert.True(snapshot1.Equals(snapshot2));
        Assert.True(snapshot1.Equals((object)snapshot2));
        Assert.True(snapshot1 == snapshot2);
        Assert.False(snapshot1 != snapshot2);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnFalseWhenAnyPropertyDiffers()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var baseSnapshot = MatchParticipantSnapshot.Create(userId1, "Player1", 0);

        var differentUserId = MatchParticipantSnapshot.Create(userId2, "Player1", 0);
        var differentName = MatchParticipantSnapshot.Create(userId1, "Player2", 0);
        var differentSeat = MatchParticipantSnapshot.Create(userId1, "Player1", 1);

        // Assert
        Assert.False(baseSnapshot.Equals(differentUserId));
        Assert.False(baseSnapshot == differentUserId);
        Assert.True(baseSnapshot != differentUserId);

        Assert.False(baseSnapshot.Equals(differentName));
        Assert.False(baseSnapshot == differentName);
        Assert.True(baseSnapshot != differentName);

        Assert.False(baseSnapshot.Equals(differentSeat));
        Assert.False(baseSnapshot == differentSeat);
        Assert.True(baseSnapshot != differentSeat);

        Assert.False(baseSnapshot.Equals(null));
        Assert.False(baseSnapshot.Equals((object?)null));
        Assert.False(baseSnapshot.Equals("SomeString"));
        Assert.False(baseSnapshot == null);
        Assert.False(null == baseSnapshot);
        Assert.True(baseSnapshot != null);
        Assert.True(null != baseSnapshot);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashCodeForEqualInstances()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var snapshot1 = MatchParticipantSnapshot.Create(userId, "Player1", 2);
        var snapshot2 = MatchParticipantSnapshot.Create(userId, "Player1", 2);

        // Assert
        Assert.Equal(snapshot1.GetHashCode(), snapshot2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsInformativeString()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var snapshot = MatchParticipantSnapshot.Create(userId, "Alice", 1);

        // Act & Assert
        Assert.Equal($"Seat 1: Alice ({userId})", snapshot.ToString());
    }

    [Fact]
    public void PublicProperties_AreImmutable()
    {
        // Arrange
        var properties = typeof(MatchParticipantSnapshot).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotEmpty(properties);
        foreach (var property in properties)
        {
            Assert.True(property.CanRead);
            Assert.False(property.CanWrite, $"Property '{property.Name}' should not have a setter.");
        }
    }
}
