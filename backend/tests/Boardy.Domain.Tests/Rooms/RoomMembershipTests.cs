using System.Reflection;
using Boardy.Domain.Rooms;

namespace Boardy.Domain.Tests.Rooms;

public class RoomMembershipTests
{
    [Fact]
    public void Create_WithHostRole_CreatesHostMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var membership = RoomMembership.Create(userId, RoomRole.Host);

        // Assert
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(RoomRole.Host, membership.Role);
        Assert.Equal(TimeSpan.Zero, membership.JoinedAt.Offset);
        Assert.True(membership.JoinedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithMemberRole_CreatesMemberMembership()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var membership = RoomMembership.Create(userId, RoomRole.Member);

        // Assert
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(RoomRole.Member, membership.Role);
        Assert.Equal(TimeSpan.Zero, membership.JoinedAt.Offset);
        Assert.True(membership.JoinedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomMembership.Create(Guid.Empty, RoomRole.Member));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(99)]
    public void Create_WithInvalidRole_ThrowsArgumentException(int invalidRoleValue)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var invalidRole = (RoomRole)invalidRoleValue;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomMembership.Create(userId, invalidRole));
    }

    [Fact]
    public void Create_RecordsValidUtcJoinedAtTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();

        // Act
        var membership = RoomMembership.Create(userId, RoomRole.Host);

        // Arrange
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(TimeSpan.Zero, membership.JoinedAt.Offset);
        Assert.InRange(membership.JoinedAt, before, after);
    }

    [Fact]
    public void Create_PreservesUserId()
    {
        // Arrange
        var expectedUserId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        // Act
        var membership = RoomMembership.Create(expectedUserId, RoomRole.Member);

        // Assert
        Assert.Equal(expectedUserId, membership.UserId);
    }

    [Fact]
    public void PublicProperties_AreImmutable()
    {
        // Arrange
        var userIdProperty = typeof(RoomMembership).GetProperty(nameof(RoomMembership.UserId));
        var roleProperty = typeof(RoomMembership).GetProperty(nameof(RoomMembership.Role));
        var joinedAtProperty = typeof(RoomMembership).GetProperty(nameof(RoomMembership.JoinedAt));

        // Assert
        Assert.NotNull(userIdProperty);
        Assert.True(userIdProperty.CanRead);
        Assert.False(userIdProperty.CanWrite, "UserId property should not have a setter.");

        Assert.NotNull(roleProperty);
        Assert.True(roleProperty.CanRead);
        Assert.False(roleProperty.CanWrite, "Role property should not have a setter.");

        Assert.NotNull(joinedAtProperty);
        Assert.True(joinedAtProperty.CanRead);
        Assert.False(joinedAtProperty.CanWrite, "JoinedAt property should not have a setter.");
    }

    [Fact]
    public void ChangeRole_WithValidRole_UpdatesRoleAndPreservesIdentityAndTimestamp()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var membership = RoomMembership.Create(userId, RoomRole.Host);
        var originalJoinedAt = membership.JoinedAt;

        // Act
        membership.ChangeRole(RoomRole.Member);

        // Assert
        Assert.Equal(RoomRole.Member, membership.Role);
        Assert.Equal(userId, membership.UserId);
        Assert.Equal(originalJoinedAt, membership.JoinedAt);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(99)]
    public void ChangeRole_WithInvalidRole_ThrowsArgumentException(int invalidRoleValue)
    {
        // Arrange
        var membership = RoomMembership.Create(Guid.NewGuid(), RoomRole.Member);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => membership.ChangeRole((RoomRole)invalidRoleValue));
    }
}

