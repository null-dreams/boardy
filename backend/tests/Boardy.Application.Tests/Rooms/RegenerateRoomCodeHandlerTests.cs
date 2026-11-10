namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;

public class RegenerateRoomCodeHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;

    public RegenerateRoomCodeHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_ThrowsArgumentNullException()
    {
        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<OperationCanceledException>(() => handler.HandleAsync(command, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(Guid.Empty, Guid.NewGuid());
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyRequesterUserId_ReturnsUserNotFound()
    {
        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(Guid.NewGuid(), Guid.Empty);
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NonexistentRoom_ReturnsRoomNotFound()
    {
        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(Guid.NewGuid(), Guid.NewGuid());
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RejectionByNonHost_ReturnsOnlyHostCanRegenerateCode()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("PRVCDE"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(room.Id, memberId);
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.OnlyHostCanRegenerateCode, result.Error);
        Assert.Equal("PRVCDE", room.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_ClosedRoom_ReturnsRoomClosed()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("PRVCDE"), RoomSettings.Default);
        room.Close(hostId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var handler = new RegenerateRoomCodeHandler(_roomRepository, _unitOfWork);
        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomClosed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulRegenerationByHost_UpdatesCodeRecordsEventAndPersists()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("PRVCDE"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var codeGenerator = new StubRoomCodeGenerator("NEWCDE");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("NEWCDE", result.Value.Value);
        Assert.Equal("NEWCDE", room.Code.Value);

        Assert.Contains(room.DomainEvents, e => e is RoomCodeRegeneratedEvent regenerated && regenerated.RoomId == room.Id);

        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal("NEWCDE", persisted.Code.Value);

        // Old code is no longer mapped
        var oldCodeLookup = await _roomRepository.GetByCodeAsync(RoomCode.Create("PRVCDE"));
        Assert.Null(oldCodeLookup);

        // New code maps to room
        var newCodeLookup = await _roomRepository.GetByCodeAsync(RoomCode.Create("NEWCDE"));
        Assert.NotNull(newCodeLookup);
        Assert.Equal(room.Id, newCodeLookup.Id);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulRegenerationByHost_WhileInMatch_Succeeds()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("PRVCDE"), RoomSettings.Default);
        room.AddMember(memberId);
        room.BeginMatch(hostId);
        room.MarkMatchInProgress();

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var codeGenerator = new StubRoomCodeGenerator("NEWCDE");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("NEWCDE", result.Value.Value);
        Assert.Equal(RoomStatus.InMatch, room.Status);
    }

    [Fact]
    public async Task HandleAsync_CollisionRetry_FindsNextAvailableCode()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("MYRMCD"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);

        // Another active room occupying TAKEN2
        var otherHostId = Guid.NewGuid();
        var otherRoom = Room.Create(otherHostId, RoomCode.Create("TAKEN2"), RoomSettings.Default);
        await _roomRepository.AddAsync(otherRoom);
        await _unitOfWork.SaveChangesAsync();

        // Generator returns current code first, then taken code, then fresh code
        var codeGenerator = new StubRoomCodeGenerator("MYRMCD", "TAKEN2", "FRSH22");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("FRSH22", result.Value.Value);
        Assert.Equal("FRSH22", room.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_ClosedRoomCodeReuse_SuccessfullyReusesCodeFromClosedRoom()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ACTVRM"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);

        // A closed room with code "REUSED"
        var oldHostId = Guid.NewGuid();
        var closedRoom = Room.Create(oldHostId, RoomCode.Create("REUSED"), RoomSettings.Default);
        closedRoom.Close(oldHostId);
        await _roomRepository.AddAsync(closedRoom);
        await _unitOfWork.SaveChangesAsync();

        var codeGenerator = new StubRoomCodeGenerator("REUSED");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("REUSED", result.Value.Value);
        Assert.Equal("REUSED", room.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_ExhaustedCandidateGeneration_ReturnsRoomCodeGenerationExhausted()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("ACTVRM"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);

        // An active room with "CLLDD2"
        var otherHostId = Guid.NewGuid();
        var otherRoom = Room.Create(otherHostId, RoomCode.Create("CLLDD2"), RoomSettings.Default);
        await _roomRepository.AddAsync(otherRoom);
        await _unitOfWork.SaveChangesAsync();

        // Stub returns exclusively conflicting codes
        var codeGenerator = new StubRoomCodeGenerator("CLLDD2", "CLLDD2", "CLLDD2", "CLLDD2", "CLLDD2");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomCodeGenerationExhausted, result.Error);
        Assert.Equal("ACTVRM", room.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_PersistenceFailure_ReturnsPersistenceFailed_AndCodeNotCommitted()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("PRVCDE"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var codeGenerator = new StubRoomCodeGenerator("NEWCDE");
        var handler = new RegenerateRoomCodeHandler(_roomRepository, codeGenerator, _unitOfWork);

        var command = new RegenerateRoomCodeCommand(room.Id, hostId);
        var result = await handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
    }
}
