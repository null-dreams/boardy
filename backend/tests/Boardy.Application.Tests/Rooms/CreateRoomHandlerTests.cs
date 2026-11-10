namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;
using Boardy.Domain.Users;

public class CreateRoomHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeRoomRepository _roomRepository;
    private readonly CryptographicRoomCodeGenerator _codeGenerator;
    private readonly CreateRoomHandler _handler;

    public CreateRoomHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _userRepository = new FakeUserRepository(_unitOfWork);
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _codeGenerator = new CryptographicRoomCodeGenerator();

        _handler = new CreateRoomHandler(
            _roomRepository,
            _userRepository,
            _codeGenerator,
            _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_SuccessfullyCreatesRoom_WithHostAssignmentAndDefaultSettings()
    {
        var host = User.CreateGuest("HostUser");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        var command = new CreateRoomCommand(host.Id);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var room = result.Value;

        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal(host.Id, room.HostUserId);
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Equal(RoomSettings.Default, room.Settings);
        Assert.NotNull(room.Code);

        // Host membership verification
        var hostMembership = Assert.Single(room.Memberships);
        Assert.Equal(host.Id, hostMembership.UserId);
        Assert.Equal(RoomRole.Host, hostMembership.Role);

        // Domain event verification
        Assert.Contains(room.DomainEvents, e => e is RoomCreatedEvent created && created.RoomId == room.Id && created.HostUserId == host.Id);

        // Persistence verification
        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(room.Id, persisted.Id);
    }

    [Fact]
    public async Task HandleAsync_AppliesCustomSettings_WhenSupplied()
    {
        var host = User.CreateGuest("HostUser2");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        var customSettings = RoomSettings.Create(maxMembers: 8, allowSpectators: true);
        var command = new CreateRoomCommand(host.Id, customSettings);

        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal(customSettings, result.Value.Settings);
    }

    [Fact]
    public async Task HandleAsync_ReturnsHostUserNotFound_WhenUserDoesNotExist()
    {
        var command = new CreateRoomCommand(Guid.NewGuid());
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.HostUserNotFound, result.Error);
        Assert.Empty(_roomRepository.CommittedRoomsById);
    }

    [Fact]
    public async Task HandleAsync_ReturnsHostUserNotFound_WhenHostUserIdIsEmpty()
    {
        var command = new CreateRoomCommand(Guid.Empty);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.HostUserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenCommandIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_RetriesOnRoomCodeCollision_AndSucceeds()
    {
        var host = User.CreateGuest("CollidingHost");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        // Pre-populate an existing room with code "AAAAAA"
        var existingRoom = Room.Create(host.Id, RoomCode.Create("AAAAAA"), RoomSettings.Default);
        await _roomRepository.AddAsync(existingRoom);
        await _unitOfWork.SaveChangesAsync();

        // Stub generator returns "AAAAAA" (collision) then "BBBBBB" (available)
        var stubGenerator = new StubRoomCodeGenerator("AAAAAA", "BBBBBB");
        var handler = new CreateRoomHandler(_roomRepository, _userRepository, stubGenerator, _unitOfWork);

        var result = await handler.HandleAsync(new CreateRoomCommand(host.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal("BBBBBB", result.Value.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_ReusesRoomCode_WhenExistingRoomIsClosed()
    {
        var oldHost = User.CreateGuest("OldHost");
        var newHost = User.CreateGuest("NewHost");
        await _userRepository.AddAsync(oldHost);
        await _userRepository.AddAsync(newHost);
        await _unitOfWork.SaveChangesAsync();

        // Create an old room with code "REUSED" and close it
        var closedRoom = Room.Create(oldHost.Id, RoomCode.Create("REUSED"), RoomSettings.Default);
        closedRoom.Close(oldHost.Id);
        await _roomRepository.AddAsync(closedRoom);
        await _unitOfWork.SaveChangesAsync();

        // Generator returns "REUSED" which belongs to a Closed room
        var stubGenerator = new StubRoomCodeGenerator("REUSED");
        var handler = new CreateRoomHandler(_roomRepository, _userRepository, stubGenerator, _unitOfWork);

        var result = await handler.HandleAsync(new CreateRoomCommand(newHost.Id));

        // Should succeed immediately without treating the closed room as a collision
        Assert.True(result.IsSuccess);
        Assert.Equal("REUSED", result.Value.Code.Value);
        Assert.Equal(newHost.Id, result.Value.HostUserId);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRoomCodeGenerationExhausted_WhenMaxRetriesExceeded()
    {
        var host = User.CreateGuest("ExhaustedHost");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        // Pre-populate an existing room with code "CLLDDD"
        var existingRoom = Room.Create(host.Id, RoomCode.Create("CLLDDD"), RoomSettings.Default);
        await _roomRepository.AddAsync(existingRoom);
        await _unitOfWork.SaveChangesAsync();

        // Stub generator always returns the colliding code
        var stubGenerator = new StubRoomCodeGenerator("CLLDDD", "CLLDDD", "CLLDDD", "CLLDDD", "CLLDDD", "CLLDDD");
        var handler = new CreateRoomHandler(_roomRepository, _userRepository, stubGenerator, _unitOfWork);

        var result = await handler.HandleAsync(new CreateRoomCommand(host.Id));

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.RoomCodeGenerationExhausted, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPersistenceFailed_AndDoesNotPersistRoom_WhenUnitOfWorkFails()
    {
        var host = User.CreateGuest("FailingHost");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var command = new CreateRoomCommand(host.Id);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
        Assert.Empty(_roomRepository.CommittedRoomsById);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new CreateRoomCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }
}
