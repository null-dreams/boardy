namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;

public class CloseRoomHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;
    private readonly CloseRoomHandler _handler;

    public CloseRoomHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _handler = new CloseRoomHandler(_roomRepository, _unitOfWork);
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

        var command = new CloseRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var command = new CloseRoomCommand(Guid.Empty, Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyRequesterUserId_ReturnsUserNotFound()
    {
        var command = new CloseRoomCommand(Guid.NewGuid(), Guid.Empty);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NonexistentRoom_ReturnsRoomNotFound()
    {
        var command = new CloseRoomCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RejectionByNonHost_ReturnsOnlyHostCanCloseRoom()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new CloseRoomCommand(room.Id, memberId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.OnlyHostCanCloseRoom, result.Error);
        Assert.Equal(RoomStatus.Open, room.Status);
    }

    [Fact]
    public async Task HandleAsync_AlreadyClosedRoom_ReturnsRoomClosed()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.Close(hostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new CloseRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomClosed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ActiveMatchClosureRejection_WhileStartingMatch_ReturnsCannotCloseDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new CloseRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotCloseDuringMatch, result.Error);
        Assert.Equal(RoomStatus.StartingMatch, room.Status);
    }

    [Fact]
    public async Task HandleAsync_ActiveMatchClosureRejection_WhileInMatch_ReturnsCannotCloseDuringMatch()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId);
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new CloseRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.CannotCloseDuringMatch, result.Error);
        Assert.Equal(RoomStatus.InMatch, room.Status);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulHostClosure_ClosesRoomRecordsEventAndPersists()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new CloseRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomStatus.Closed, result.Value.Status);
        Assert.Equal(RoomStatus.Closed, room.Status);

        Assert.Contains(room.DomainEvents, e => e is RoomClosedEvent closed && closed.RoomId == room.Id);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(RoomStatus.Closed, persisted.Status);
    }

    [Fact]
    public async Task HandleAsync_PersistenceFailure_ReturnsPersistenceFailed()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var command = new CloseRoomCommand(room.Id, hostId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
    }
}
