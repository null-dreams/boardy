namespace Boardy.Application.Tests.Matches;

using Boardy.Application.Matches;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Matches.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Matches;
using Boardy.Domain.Matches.Events;
using Boardy.Domain.Rooms;
using Boardy.Domain.Rooms.Events;
using Boardy.Domain.Users;

public class StartMatchHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;
    private readonly FakeMatchRepository _matchRepository;
    private readonly FakeUserRepository _userRepository;
    private readonly StartMatchHandler _handler;

    public StartMatchHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _matchRepository = new FakeMatchRepository(_unitOfWork);
        _userRepository = new FakeUserRepository(_unitOfWork);

        _handler = new StartMatchHandler(
            _roomRepository,
            _matchRepository,
            _userRepository,
            _unitOfWork);
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

        var command = new StartMatchCommand(Guid.NewGuid(), Guid.NewGuid(), "catan-standard");
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(command, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var command = new StartMatchCommand(Guid.Empty, Guid.NewGuid(), "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyHostUserId_ReturnsUserNotFound()
    {
        var command = new StartMatchCommand(Guid.NewGuid(), Guid.Empty, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.UserNotFound, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID-UPPER")]
    [InlineData("-starts-with-hyphen")]
    [InlineData("ends-with-hyphen-")]
    [InlineData("double--hyphen")]
    public async Task HandleAsync_InvalidGameDefinitionId_ReturnsInvalidGameDefinitionId(string invalidId)
    {
        var command = new StartMatchCommand(Guid.NewGuid(), Guid.NewGuid(), invalidId);
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.InvalidGameDefinitionId, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RoomNotFound_ReturnsRoomNotFound()
    {
        var command = new StartMatchCommand(Guid.NewGuid(), Guid.NewGuid(), "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NonHostRequester_ReturnsOnlyHostCanStartMatch()
    {
        var host = User.CreateGuest("HostUser");
        var member = User.CreateGuest("MemberUser");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member);

        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(member.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new StartMatchCommand(room.Id, member.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.OnlyHostCanStartMatch, result.Error);
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Null(room.ActiveMatchId);
    }

    [Theory]
    [InlineData(RoomStatus.Closed)]
    [InlineData(RoomStatus.StartingMatch)]
    [InlineData(RoomStatus.InMatch)]
    public async Task HandleAsync_RoomNotOpen_ReturnsRoomNotOpen(RoomStatus status)
    {
        var host = User.CreateGuest("HostUser");
        await _userRepository.AddAsync(host);

        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        if (status == RoomStatus.Closed)
        {
            room.Close(host.Id);
        }
        else if (status == RoomStatus.StartingMatch)
        {
            room.BeginMatch(host.Id, Guid.NewGuid());
        }
        else if (status == RoomStatus.InMatch)
        {
            room.BeginMatch(host.Id, Guid.NewGuid());
            room.MarkMatchInProgress();
        }

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new StartMatchCommand(room.Id, host.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.RoomNotOpen, result.Error);
    }

    [Fact]
    public async Task HandleAsync_MissingParticipantUser_ReturnsParticipantUserNotFound()
    {
        var host = User.CreateGuest("HostUser");
        await _userRepository.AddAsync(host);

        // Member exists in room, but not in user repository
        var missingMemberId = Guid.NewGuid();
        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(missingMemberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new StartMatchCommand(room.Id, host.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.ParticipantUserNotFound, result.Error);
        Assert.Equal(RoomStatus.Open, room.Status);
        Assert.Null(room.ActiveMatchId);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulActivation_ActivatesMatchAndStartsRoom()
    {
        var host = User.CreateGuest("AliceHost");
        var member1 = User.CreateGuest("BobPlayer");
        var member2 = User.CreateGuest("CharliePlayer");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member1);
        await _userRepository.AddAsync(member2);

        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(member1.Id);
        room.AddMember(member2.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        room.ClearDomainEvents();

        var command = new StartMatchCommand(room.Id, host.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var matchResult = result.Value;

        Assert.NotEqual(Guid.Empty, matchResult.MatchId);
        Assert.Equal(room.Id, matchResult.RoomId);
        Assert.Equal("catan-standard", matchResult.GameDefinitionId);
        Assert.Equal(MatchStatus.Active, matchResult.Status);
        Assert.Equal(3, matchResult.Participants.Count);

        // Deterministic seat order verification
        Assert.Equal(0, matchResult.Participants[0].SeatOrder);
        Assert.Equal(host.Id, matchResult.Participants[0].UserId);
        Assert.Equal("AliceHost", matchResult.Participants[0].DisplayName);

        Assert.Equal(1, matchResult.Participants[1].SeatOrder);
        Assert.Equal(member1.Id, matchResult.Participants[1].UserId);
        Assert.Equal("BobPlayer", matchResult.Participants[1].DisplayName);

        Assert.Equal(2, matchResult.Participants[2].SeatOrder);
        Assert.Equal(member2.Id, matchResult.Participants[2].UserId);
        Assert.Equal("CharliePlayer", matchResult.Participants[2].DisplayName);

        // Room state verification
        Assert.Equal(RoomStatus.InMatch, room.Status);
        Assert.Equal(matchResult.MatchId, room.ActiveMatchId);

        // Room domain events verification & ordering
        var roomEvents = room.DomainEvents.ToList();
        Assert.Equal(2, roomEvents.Count);
        var roomInitEvt = Assert.IsType<RoomMatchInitializationStartedEvent>(roomEvents[0]);
        Assert.Equal(room.Id, roomInitEvt.RoomId);
        Assert.Equal(matchResult.MatchId, roomInitEvt.MatchId);

        var roomStartedEvt = Assert.IsType<RoomMatchStartedEvent>(roomEvents[1]);
        Assert.Equal(room.Id, roomStartedEvt.RoomId);
        Assert.Equal(matchResult.MatchId, roomStartedEvt.MatchId);

        // Repository persistence verification
        var persistedMatch = await _matchRepository.GetByIdAsync(matchResult.MatchId);
        Assert.NotNull(persistedMatch);
        Assert.Equal(MatchStatus.Active, persistedMatch.Status);
        Assert.NotNull(persistedMatch.StartedAt);
        Assert.Equal(3, persistedMatch.Participants.Count);

        // Match domain events verification & ordering
        var matchEvents = persistedMatch.DomainEvents.ToList();
        Assert.Equal(3, matchEvents.Count);
        var matchCreatedEvt = Assert.IsType<MatchCreatedEvent>(matchEvents[0]);
        Assert.Equal(matchResult.MatchId, matchCreatedEvt.MatchId);
        Assert.Equal("catan-standard", matchCreatedEvt.GameDefinitionId.Value);

        var matchInitEvt = Assert.IsType<MatchInitializationStartedEvent>(matchEvents[1]);
        Assert.Equal(matchResult.MatchId, matchInitEvt.MatchId);

        var matchStartedEvt = Assert.IsType<MatchStartedEvent>(matchEvents[2]);
        Assert.Equal(matchResult.MatchId, matchStartedEvt.MatchId);

        var persistedRoom = await _roomRepository.GetByIdAsync(room.Id);
        Assert.NotNull(persistedRoom);
        Assert.Equal(RoomStatus.InMatch, persistedRoom.Status);
        Assert.Equal(matchResult.MatchId, persistedRoom.ActiveMatchId);
    }

    [Fact]
    public async Task HandleAsync_DeterministicSeatOrder_OrdersByJoinedAtThenUserId()
    {
        var host = User.CreateGuest("Host");
        var member1 = User.CreateGuest("Member1");
        var member2 = User.CreateGuest("Member2");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member1);
        await _userRepository.AddAsync(member2);

        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(member1.Id);
        room.AddMember(member2.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var command = new StartMatchCommand(room.Id, host.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.True(result.IsSuccess);
        var participants = result.Value.Participants;

        // Seat 0 must be host (joined first at room creation)
        Assert.Equal(0, participants[0].SeatOrder);
        Assert.Equal(host.Id, participants[0].UserId);

        // Subsequent seats follow join order
        Assert.Equal(1, participants[1].SeatOrder);
        Assert.Equal(member1.Id, participants[1].UserId);

        Assert.Equal(2, participants[2].SeatOrder);
        Assert.Equal(member2.Id, participants[2].UserId);
    }

    [Fact]
    public async Task HandleAsync_PersistenceFailure_ReturnsPersistenceFailed_AndNothingCommitted()
    {
        var host = User.CreateGuest("HostUser");
        var member = User.CreateGuest("MemberUser");
        await _userRepository.AddAsync(host);
        await _userRepository.AddAsync(member);

        var room = Room.Create(host.Id, RoomCode.Create("ABC234"), RoomSettings.Default);
        room.AddMember(member.Id);
        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        _unitOfWork.ShouldFail = true;

        var command = new StartMatchCommand(room.Id, host.Id, "catan-standard");
        var result = await _handler.HandleAsync(command);

        Assert.False(result.IsSuccess);
        Assert.Equal(MatchErrors.PersistenceFailed, result.Error);

        _unitOfWork.ShouldFail = false;

        // Committed match dictionary in repository is empty (no match was committed)
        Assert.Empty(_matchRepository.CommittedMatchesById);
    }
}
