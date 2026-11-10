namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;

public class LeaveRoomHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;
    private readonly LeaveRoomHandler _handler;

    public LeaveRoomHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _handler = new LeaveRoomHandler(_roomRepository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new LeaveRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var command = new LeaveRoomCommand(Guid.Empty, Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyUserId_ReturnsUserNotFound()
    {
        var command = new LeaveRoomCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RoomNotFound_ReturnsRoomNotFound()
    {
        var command = new LeaveRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RoomClosed_ReturnsRoomClosed()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.RemoveMember(memberId);
        room.Close(hostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomClosed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_UserNotMember_ReturnsUserNotMember()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var nonMemberId = Guid.NewGuid();
        var command = new LeaveRoomCommand(room.Id, nonMemberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RegularMemberLeaves_SuccessfullyRemovesMemberAndPersists()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;

        Assert.Single(updatedRoom.Memberships);
        Assert.DoesNotContain(updatedRoom.Memberships, m => m.UserId == memberId);
        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomMemberLeftEvent left && left.RoomId == room.Id && left.UserId == memberId);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Single(persisted.Memberships);
        Assert.DoesNotContain(persisted.Memberships, m => m.UserId == memberId);
    }

    [Fact]
    public async Task HandleAsync_RegularMemberLeaves_WhileStartingMatch_ReturnsCannotLeaveDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId, Guid.NewGuid());

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new LeaveRoomCommand(room.Id, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotLeaveDuringMatch, result.Error);

        Assert.Equal(2, room.Memberships.Count);
        Assert.Contains(room.Memberships, m => m.UserId == memberId);
        Assert.DoesNotContain(room.DomainEvents, e => e is RoomMemberLeftEvent);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Memberships.Count);
        Assert.Contains(persisted.Memberships, m => m.UserId == memberId);
    }

    [Fact]
    public async Task HandleAsync_RegularMemberLeaves_WhileInMatch_ReturnsCannotLeaveDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId, Guid.NewGuid());
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new LeaveRoomCommand(room.Id, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotLeaveDuringMatch, result.Error);

        Assert.Equal(2, room.Memberships.Count);
        Assert.Contains(room.Memberships, m => m.UserId == memberId);
        Assert.DoesNotContain(room.DomainEvents, e => e is RoomMemberLeftEvent);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Memberships.Count);
        Assert.Contains(persisted.Memberships, m => m.UserId == memberId);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_SoleMember_SuccessfullyClosesRoom()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;

        Assert.Equal(RoomStatus.Closed, updatedRoom.Status);
        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomClosedEvent closed && closed.RoomId == room.Id);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(RoomStatus.Closed, persisted.Status);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_SoleMember_WhileInMatch_ReturnsCannotLeaveDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.BeginMatch(hostId, Guid.NewGuid());
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new LeaveRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotLeaveDuringMatch, result.Error);
        Assert.Equal(RoomStatus.InMatch, room.Status);
        Assert.Single(room.Memberships);
        Assert.DoesNotContain(room.DomainEvents, e => e is RoomClosedEvent or RoomMemberLeftEvent);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(RoomStatus.InMatch, persisted.Status);
        Assert.Single(persisted.Memberships);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_WithoutNewHost_ReturnsHostCannotLeaveWithoutTransfer()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: null);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.HostCannotLeaveWithoutTransfer, result.Error);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_NewHostIsSelf_ReturnsCannotTransferHostToSelf()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotTransferHostToSelf, result.Error);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_NewHostNotMember_ReturnsProposedHostNotMember()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: nonMemberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.ProposedHostNotMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_WithValidNewHost_TransfersHostAndRemovesOldHost()
    {
        var hostId = Guid.NewGuid();
        var newHostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(newHostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: newHostId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;

        Assert.Equal(newHostId, updatedRoom.HostUserId);
        Assert.Single(updatedRoom.Memberships);
        var remainingMember = Assert.Single(updatedRoom.Memberships);
        Assert.Equal(newHostId, remainingMember.UserId);
        Assert.Equal(RoomRole.Host, remainingMember.Role);

        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomHostTransferredEvent transferred &&
                                                      transferred.PreviousHostUserId == hostId &&
                                                      transferred.NewHostUserId == newHostId);
        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomMemberLeftEvent left &&
                                                      left.UserId == hostId);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(newHostId, persisted.HostUserId);
        Assert.Single(persisted.Memberships);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_WithValidNewHost_WhileInMatch_ReturnsCannotLeaveDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var newHostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(newHostId);
        room.BeginMatch(hostId, Guid.NewGuid());
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: newHostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotLeaveDuringMatch, result.Error);

        Assert.Equal(RoomStatus.InMatch, room.Status);
        Assert.Equal(hostId, room.HostUserId);
        Assert.Equal(2, room.Memberships.Count);
        Assert.DoesNotContain(room.DomainEvents, e => e is RoomHostTransferredEvent or RoomMemberLeftEvent);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(RoomStatus.InMatch, persisted.Status);
        Assert.Equal(hostId, persisted.HostUserId);
        Assert.Equal(2, persisted.Memberships.Count);
    }

    [Fact]
    public async Task HandleAsync_HostLeaves_MultipleMembers_WhileStartingMatch_ReturnsCannotLeaveDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var newHostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(newHostId);
        room.BeginMatch(hostId, Guid.NewGuid());

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new LeaveRoomCommand(room.Id, hostId, NewHostUserId: newHostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotLeaveDuringMatch, result.Error);

        Assert.Equal(RoomStatus.StartingMatch, room.Status);
        Assert.Equal(hostId, room.HostUserId);
        Assert.Equal(2, room.Memberships.Count);
        Assert.DoesNotContain(room.DomainEvents, e => e is RoomHostTransferredEvent or RoomMemberLeftEvent);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(RoomStatus.StartingMatch, persisted.Status);
        Assert.Equal(hostId, persisted.HostUserId);
        Assert.Equal(2, persisted.Memberships.Count);
    }

    [Fact]
    public async Task HandleAsync_PersistenceFailure_ReturnsPersistenceFailed()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var command = new LeaveRoomCommand(room.Id, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
    }
}
