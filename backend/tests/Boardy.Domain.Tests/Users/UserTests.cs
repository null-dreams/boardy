using Boardy.Domain.Users;

namespace Boardy.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void CreateGuest_WithValidDisplayName_CreatesGuestUser()
    {
        // Act
        var user = User.CreateGuest("GuestPlayer");

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("GuestPlayer", user.DisplayName);
        Assert.Equal(UserKind.Guest, user.Kind);
        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
        Assert.True(user.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateRegistered_WithValidDisplayName_CreatesRegisteredUser()
    {
        // Act
        var user = User.CreateRegistered("RegisteredPlayer");

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("RegisteredPlayer", user.DisplayName);
        Assert.Equal(UserKind.Registered, user.Kind);
        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
        Assert.True(user.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData("  Alice  ", "Alice")]
    [InlineData("\tBob\n", "Bob")]
    [InlineData("Charlie", "Charlie")]
    public void Create_TrimsWhitespaceFromDisplayName(string rawName, string expectedTrimmedName)
    {
        // Act
        var guest = User.CreateGuest(rawName);
        var registered = User.CreateRegistered(rawName);

        // Assert
        Assert.Equal(expectedTrimmedName, guest.DisplayName);
        Assert.Equal(expectedTrimmedName, registered.DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void CreateGuest_WithNullOrWhitespaceDisplayName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => User.CreateGuest(invalidName!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void CreateRegistered_WithNullOrWhitespaceDisplayName_ThrowsArgumentException(string? invalidName)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => User.CreateRegistered(invalidName!));
    }

    [Fact]
    public void Create_GeneratesUniqueAndNonEmptyIds()
    {
        // Act
        var user1 = User.CreateGuest("Player1");
        var user2 = User.CreateGuest("Player2");
        var user3 = User.CreateRegistered("Player3");

        // Assert
        Assert.NotEqual(Guid.Empty, user1.Id);
        Assert.NotEqual(Guid.Empty, user2.Id);
        Assert.NotEqual(Guid.Empty, user3.Id);
        Assert.NotEqual(user1.Id, user2.Id);
        Assert.NotEqual(user1.Id, user3.Id);
        Assert.NotEqual(user2.Id, user3.Id);
    }

    [Fact]
    public void Create_RecordsValidUtcCreationTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var user = User.CreateRegistered("PlayerUtc");

        // Arrange
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(TimeSpan.Zero, user.CreatedAt.Offset);
        Assert.InRange(user.CreatedAt, before, after);
    }
}

