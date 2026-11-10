namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;

public class TransferHostHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;
    private readonly TransferHostHandler _handler;

    public TransferHostHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _handler = new TransferHostHandler(_roomRepository, _unitOfWork);
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

        var command = new TransferHostCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var command = new TransferHostCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyRequesterUserId_ReturnsUserNotFound()
    {
        var command = new TransferHostCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyNewHostUserId_ReturnsProposedHostNotMember()
    {
        var command = new TransferHostCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.ProposedHostNotMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RoomNotFound_ReturnsRoomNotFound()
    {
        var command = new TransferHostCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
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

        var command = new TransferHostCommand(room.Id, hostId, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomClosed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RequesterNotHost_ReturnsOnlyHostCanTransferHost()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var thirdMemberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.AddMember(thirdMemberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        // Non-host attempts to transfer host to third member
        var command = new TransferHostCommand(room.Id, memberId, thirdMemberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.OnlyHostCanTransferHost, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NewHostIsCurrentHost_ReturnsCannotTransferHostToSelf()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new TransferHostCommand(room.Id, hostId, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotTransferHostToSelf, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NewHostNotMember_ReturnsProposedHostNotMember()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new TransferHostCommand(room.Id, hostId, nonMemberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.ProposedHostNotMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_SuccessfullyTransfersHostAndPersists()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new TransferHostCommand(room.Id, hostId, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;

        Assert.Equal(memberId, updatedRoom.HostUserId);
        Assert.Equal(2, updatedRoom.Memberships.Count);

        var oldHostMembership = Assert.Single(updatedRoom.Memberships, m => m.UserId == hostId);
        Assert.Equal(RoomRole.Member, oldHostMembership.Role);

        var newHostMembership = Assert.Single(updatedRoom.Memberships, m => m.UserId == memberId);
        Assert.Equal(RoomRole.Host, newHostMembership.Role);

        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomHostTransferredEvent transferred &&
                                                      transferred.PreviousHostUserId == hostId &&
                                                      transferred.NewHostUserId == memberId);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(memberId, persisted.HostUserId);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_WhileStartingMatch_SuccessfullyTransfersHost()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new TransferHostCommand(room.Id, hostId, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;
        Assert.Equal(RoomStatus.StartingMatch, updatedRoom.Status);
        Assert.Equal(memberId, updatedRoom.HostUserId);
    }

    [Fact]
    public async Task HandleAsync_ValidTransfer_WhileInMatch_SuccessfullyTransfersHost()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId);
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new TransferHostCommand(room.Id, hostId, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;
        Assert.Equal(RoomStatus.InMatch, updatedRoom.Status);
        Assert.Equal(memberId, updatedRoom.HostUserId);
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

        var command = new TransferHostCommand(room.Id, hostId, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
    }
}
