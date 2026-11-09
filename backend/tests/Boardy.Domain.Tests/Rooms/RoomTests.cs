using System.Collections;
using System.Reflection;
using Boardy.Domain.Rooms;

namespace Boardy.Domain.Tests.Rooms;

public class RoomTests
{
    [Fact]
    public void Create_WithValidParameters_SuccessfullyCreatesRoom()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act
        var room = Room.Create(hostUserId, code, settings);

        // Assert
        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal(code, room.Code);
        Assert.Equal(hostUserId, room.HostUserId);
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Equal(settings, room.Settings);
        Assert.Equal(TimeSpan.Zero, room.CreatedAt.Offset);
        Assert.True(room.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_GeneratesNonEmptyUniqueRoomId()
    {
        // Arrange
        var host1 = Guid.NewGuid();
        var host2 = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act
        var room1 = Room.Create(host1, code, settings);
        var room2 = Room.Create(host2, code, settings);

        // Assert
        Assert.NotEqual(Guid.Empty, room1.Id);
        Assert.NotEqual(Guid.Empty, room2.Id);
        Assert.NotEqual(room1.Id, room2.Id);
    }

    [Fact]
    public void Create_EstablishesInitialHostIdentityAndMembership()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act
        var room = Room.Create(hostUserId, code, settings);

        // Assert
        Assert.Equal(hostUserId, room.HostUserId);
        Assert.Single(room.Memberships);

        var hostMembership = room.Memberships.Single();
        Assert.Equal(hostUserId, hostMembership.UserId);
        Assert.Equal(RoomRole.Host, hostMembership.Role);
        Assert.Equal(TimeSpan.Zero, hostMembership.JoinedAt.Offset);
    }

    [Fact]
    public void Create_SetsInitialStatusToOpen()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act
        var room = Room.Create(hostUserId, code, settings);

        // Assert
        Assert.Equal(RoomStatus.Open, room.Status);
    }

    [Fact]
    public void Create_PreservesRoomCodeAndRoomSettings()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("XYZ789");
        var settings = RoomSettings.Create(8, allowSpectators: true);

        // Act
        var room = Room.Create(hostUserId, code, settings);

        // Assert
        Assert.Same(code, room.Code);
        Assert.Same(settings, room.Settings);
    }

    [Fact]
    public void Create_RecordsValidUtcCreationTimestamp()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act
        var room = Room.Create(hostUserId, code, settings);

        // Arrange
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(TimeSpan.Zero, room.CreatedAt.Offset);
        Assert.InRange(room.CreatedAt, before, after);
    }

    [Fact]
    public void Create_WithEmptyHostUserId_ThrowsArgumentException()
    {
        // Arrange
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Room.Create(Guid.Empty, code, settings));
    }

    [Fact]
    public void Create_WithNullRoomCode_ThrowsArgumentNullException()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var settings = RoomSettings.Default;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Room.Create(hostUserId, null!, settings));
    }

    [Fact]
    public void Create_WithNullRoomSettings_ThrowsArgumentNullException()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Room.Create(hostUserId, code, null!));
    }

    [Fact]
    public void Memberships_CannotBeExternallyModified()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var code = RoomCode.Create("ABC234");
        var settings = RoomSettings.Default;
        var room = Room.Create(hostUserId, code, settings);

        // Act & Assert
        if (room.Memberships is IList<RoomMembership> list)
        {
            Assert.Throws<NotSupportedException>(() => list.Add(RoomMembership.Create(Guid.NewGuid(), RoomRole.Member)));
        }

        if (room.Memberships is IList nonGenericList)
        {
            Assert.True(nonGenericList.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => nonGenericList.Add(RoomMembership.Create(Guid.NewGuid(), RoomRole.Member)));
        }
    }

    [Fact]
    public void PublicProperties_DoNotExposePublicSetters()
    {
        // Arrange
        var properties = typeof(Room).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotEmpty(properties);
        foreach (var property in properties)
        {
            Assert.True(property.SetMethod == null || !property.SetMethod.IsPublic,
                $"Property '{property.Name}' should not have a public setter.");
        }
    }
}
