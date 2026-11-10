namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;
using Boardy.Domain.Users;

public class JoinRoomHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository;
    private readonly FakeRoomRepository _roomRepository;
    private readonly JoinRoomHandler _handler;

    public JoinRoomHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _userRepository = new FakeUserRepository(_unitOfWork);
        _roomRepository = new FakeRoomRepository(_unitOfWork);

        _handler = new JoinRoomHandler(
            _roomRepository,
            _userRepository,
            _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_SuccessfullyAddsMemberToRoom()
    {
        var host = User.CreateGuest("HostUser");
        var joiningUser = User.CreateGuest("JoiningUser");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(joiningUser);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(joiningUser.Id, "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var updatedRoom = result.Value;

        Assert.Equal(2, updatedRoom.Memberships.Count);
        var newMembership = Assert.Single(updatedRoom.Memberships, m => m.UserId == joiningUser.Id);
        Assert.Equal(RoomRole.Member, newMembership.Role);

        // Domain event verification
        Assert.Contains(updatedRoom.DomainEvents, e => e is RoomMemberJoinedEvent joined && joined.RoomId == room.Id && joined.UserId == joiningUser.Id);

        // Persistence verification
        var persisted = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Memberships.Count);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRoomNotFound_WhenRoomCodeDoesNotExist()
    {
        var user = User.CreateGuest("User1");
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(user.Id, "UNKWNN");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("SHORT")]
    [InlineData("TOOLONGCODE")]
    [InlineData("000000")] // Contains ambiguous 0
    [InlineData("IIIIII")] // Contains ambiguous I
    [InlineData("ABC!@#")] // Contains non-alphanumeric
    [InlineData(" ABCDEF")] // Leading whitespace
    [InlineData("ABCDEF ")] // Trailing whitespace
    [InlineData(" ABCDEF ")] // Surrounding whitespace
    public async Task HandleAsync_ReturnsInvalidRoomCode_WhenRoomCodeIsMalformed(string invalidCode)
    {
        var user = User.CreateGuest("User2");
        await _userRepository.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(user.Id, invalidCode);
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.InvalidRoomCode, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NormalizesLowercaseRoomCode_AndSucceeds()
    {
        var host = User.CreateGuest("Host");
        var joiningUser = User.CreateGuest("Joiner");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(joiningUser);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        // Pass lowercase room code "abcdef"
        var command = new JoinRoomCommand(joiningUser.Id, "abcdef");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        Assert.Equal("ABCDEF", result.Value.Code.Value);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUserNotFound_WhenUserDoesNotExist()
    {
        var command = new JoinRoomCommand(Guid.NewGuid(), "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUserNotFound_WhenUserIdIsEmpty()
    {
        var command = new JoinRoomCommand(Guid.Empty, "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ThrowsArgumentNullException_WhenCommandIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_ReturnsUserAlreadyMember_WhenHostTriesToJoinAgain()
    {
        var host = User.CreateGuest("HostUser");
        await _userRepository.AddAsync(host);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(host.Id, "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.UserAlreadyMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUserAlreadyMember_WhenMemberAttemptsDuplicateJoin()
    {
        var host = User.CreateGuest("Host");
        var member = User.CreateGuest("Member");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(member.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(member.Id, "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.UserAlreadyMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRoomFull_WhenRoomIsAtMaximumCapacity()
    {
        var host = User.CreateGuest("Host");
        var member1 = User.CreateGuest("Member1");
        var member2 = User.CreateGuest("Member2");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member1);
        await _userRepository.AddAsync(member2);
        await _unitOfWork.SaveChangesAsync();

        // Room max capacity = 2 (Host + Member1)
        var settings = RoomSettings.Create(maxMembers: 2);
        var room = Room.Create(host.Id, RoomCode.Create("FULLRM"), settings);
        room.AddMember(member1.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(member2.Id, "FULLRM");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.RoomFull, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRoomNotOpen_WhenRoomIsClosed()
    {
        var host = User.CreateGuest("Host");
        var member = User.CreateGuest("Member");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("CLSDRM"), RoomSettings.Default);
        room.Close(host.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new JoinRoomCommand(member.Id, "CLSDRM");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.RoomNotOpen, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPersistenceFailed_WhenUnitOfWorkFails()
    {
        var host = User.CreateGuest("Host");
        var member = User.CreateGuest("Member");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member);
        await _unitOfWork.SaveChangesAsync();

        var room = Room.Create(host.Id, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var command = new JoinRoomCommand(member.Id, "ABCDEF");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsFailure);
        Assert.Equal(RoomErrors.PersistenceFailed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var command = new JoinRoomCommand(Guid.NewGuid(), "ABCDEF");

        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }
}
