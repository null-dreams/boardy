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
    public void AddMember_WithValidUserId_AddsMemberAndReturnsMembership()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act
        var membership = room.AddMember(memberUserId);

        // Assert
        Assert.NotNull(membership);
        Assert.Equal(memberUserId, membership.UserId);
        Assert.Equal(RoomRole.Member, membership.Role);
        Assert.Equal(TimeSpan.Zero, membership.JoinedAt.Offset);
        Assert.Equal(2, room.Memberships.Count);
        Assert.Contains(membership, room.Memberships);
    }

    [Fact]
    public void AddMember_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => room.AddMember(Guid.Empty));
    }

    [Fact]
    public void AddMember_WhenUserIsAlreadyMemberOrHost_ThrowsInvalidOperationException()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(memberUserId);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.AddMember(hostUserId));
        Assert.Throws<InvalidOperationException>(() => room.AddMember(memberUserId));
    }

    [Theory]
    [InlineData(RoomStatus.StartingMatch)]
    [InlineData(RoomStatus.InMatch)]
    [InlineData(RoomStatus.Closed)]
    public void AddMember_WhenRoomIsNotOpen_ThrowsInvalidOperationException(RoomStatus nonOpenStatus)
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);
        typeof(Room).GetProperty(nameof(Room.Status))!.SetValue(room, nonOpenStatus);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.AddMember(Guid.NewGuid()));
    }

    [Fact]
    public void AddMember_EnforcesMaxMembersCapacity_AndHostCountsTowardCapacity()
    {
        // Arrange: capacity = 2 (host + 1 member)
        var hostUserId = Guid.NewGuid();
        var settings = RoomSettings.Create(maxMembers: 2);
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), settings);
        var member1 = Guid.NewGuid();
        var member2 = Guid.NewGuid();

        // Act: adding 1st member brings membership count to 2 (host + member1)
        room.AddMember(member1);

        // Assert & Act: attempting to add 2nd member exceeds MaxMembers capacity
        Assert.Equal(2, room.Memberships.Count);
        Assert.Throws<InvalidOperationException>(() => room.AddMember(member2));
    }

    [Fact]
    public void RemoveMember_WithValidMemberId_SuccessfullyRemovesMember()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(memberUserId);
        Assert.Equal(2, room.Memberships.Count);

        // Act
        room.RemoveMember(memberUserId);

        // Assert
        Assert.Single(room.Memberships);
        Assert.DoesNotContain(room.Memberships, m => m.UserId == memberUserId);
    }

    [Fact]
    public void RemoveMember_WithEmptyUserId_ThrowsArgumentException()
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => room.RemoveMember(Guid.Empty));
    }

    [Fact]
    public void RemoveMember_WithUnknownUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.RemoveMember(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveMember_WithHostUserId_ThrowsInvalidOperationException()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.RemoveMember(hostUserId));
    }

    [Theory]
    [InlineData(RoomStatus.StartingMatch)]
    [InlineData(RoomStatus.InMatch)]
    public void RemoveMember_WhenRoomIsInMatchOrStartingMatch_SuccessfullyRemovesMember(RoomStatus activeStatus)
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(memberUserId);

        typeof(Room).GetProperty(nameof(Room.Status))!.SetValue(room, activeStatus);

        // Act
        room.RemoveMember(memberUserId);

        // Assert
        Assert.Single(room.Memberships);
        Assert.DoesNotContain(room.Memberships, m => m.UserId == memberUserId);
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
    public void Memberships_RemainsNonMutableAfterAddingAndRemovingMembers()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);
        var liveMemberships = room.Memberships;

        // Act & Assert before add
        Assert.Throws<NotSupportedException>(() => ((IList<RoomMembership>)liveMemberships).Add(RoomMembership.Create(Guid.NewGuid(), RoomRole.Member)));

        // Act add
        room.AddMember(memberUserId);
        Assert.Equal(2, liveMemberships.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<RoomMembership>)liveMemberships).Add(RoomMembership.Create(Guid.NewGuid(), RoomRole.Member)));

        // Act remove
        room.RemoveMember(memberUserId);
        Assert.Single(liveMemberships);
        Assert.Throws<NotSupportedException>(() => ((IList<RoomMembership>)liveMemberships).Add(RoomMembership.Create(Guid.NewGuid(), RoomRole.Member)));
    }

    [Fact]
    public void TransferHost_WithValidMember_SuccessfullyTransfersHostRole()
    {
        // Arrange
        var oldHostId = Guid.NewGuid();
        var newHostId = Guid.NewGuid();
        var room = Room.Create(oldHostId, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(newHostId);

        var oldHostMembership = room.Memberships.Single(m => m.UserId == oldHostId);
        var newHostMembership = room.Memberships.Single(m => m.UserId == newHostId);
        var oldHostJoinedAt = oldHostMembership.JoinedAt;
        var newHostJoinedAt = newHostMembership.JoinedAt;

        // Act
        room.TransferHost(newHostId);

        // Assert
        Assert.Equal(newHostId, room.HostUserId);
        Assert.Equal(RoomRole.Member, oldHostMembership.Role);
        Assert.Equal(RoomRole.Host, newHostMembership.Role);
        Assert.Single(room.Memberships, m => m.Role == RoomRole.Host);
        Assert.Equal(2, room.Memberships.Count);
        Assert.Equal(oldHostJoinedAt, oldHostMembership.JoinedAt);
        Assert.Equal(newHostJoinedAt, newHostMembership.JoinedAt);
    }

    [Fact]
    public void TransferHost_WithEmptyNewHostUserId_ThrowsArgumentException()
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => room.TransferHost(Guid.Empty));
    }

    [Fact]
    public void TransferHost_WithCurrentHost_ThrowsInvalidOperationException()
    {
        // Arrange
        var hostUserId = Guid.NewGuid();
        var room = Room.Create(hostUserId, RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(hostUserId));
    }

    [Fact]
    public void TransferHost_WithUnknownUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var room = Room.Create(Guid.NewGuid(), RoomCode.Create("ABC234"), RoomSettings.Default);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(Guid.NewGuid()));
    }

    [Fact]
    public void TransferHost_WhenValidationFails_LeavesAggregateUnchanged()
    {
        // Arrange
        var oldHostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(oldHostId, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(memberId);

        var oldHostMembership = room.Memberships.Single(m => m.UserId == oldHostId);
        var memberMembership = room.Memberships.Single(m => m.UserId == memberId);

        // Act & Assert (empty Guid)
        Assert.Throws<ArgumentException>(() => room.TransferHost(Guid.Empty));
        Assert.Equal(oldHostId, room.HostUserId);
        Assert.Equal(RoomRole.Host, oldHostMembership.Role);
        Assert.Equal(RoomRole.Member, memberMembership.Role);

        // Act & Assert (current host)
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(oldHostId));
        Assert.Equal(oldHostId, room.HostUserId);
        Assert.Equal(RoomRole.Host, oldHostMembership.Role);
        Assert.Equal(RoomRole.Member, memberMembership.Role);

        // Act & Assert (unknown user)
        Assert.Throws<InvalidOperationException>(() => room.TransferHost(Guid.NewGuid()));
        Assert.Equal(oldHostId, room.HostUserId);
        Assert.Equal(RoomRole.Host, oldHostMembership.Role);
        Assert.Equal(RoomRole.Member, memberMembership.Role);
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
