namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;
using Boardy.Application.Tests.Identity.TestDoubles;
using Boardy.Application.Tests.Rooms.TestDoubles;
using Boardy.Domain.Rooms;

public class GetRoomDetailsHandlerTests
{
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeRoomRepository _roomRepository;
    private readonly GetRoomDetailsHandler _handler;

    public GetRoomDetailsHandlerTests()
    {
        _unitOfWork = new FakeUnitOfWork();
        _roomRepository = new FakeRoomRepository(_unitOfWork);
        _handler = new GetRoomDetailsHandler(_roomRepository);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.HandleAsync(null!));
    }

    [Fact]
    public async Task HandleAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = new GetRoomDetailsQuery(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.HandleAsync(query, cts.Token));
    }

    [Fact]
    public async Task HandleAsync_EmptyRoomId_ReturnsRoomNotFound()
    {
        var query = new GetRoomDetailsQuery(Guid.Empty, Guid.NewGuid());
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_EmptyRequesterUserId_ReturnsUserNotFound()
    {
        var query = new GetRoomDetailsQuery(Guid.NewGuid(), Guid.Empty);
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_NonexistentRoom_ReturnsRoomNotFound()
    {
        var query = new GetRoomDetailsQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.RoomNotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_RejectionByNonMember_ReturnsUserNotMember()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var nonMemberId = Guid.NewGuid();

        var room = Room.Create(hostId, RoomCode.Create("ABCDEF"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetRoomDetailsQuery(room.Id, nonMemberId);
        var result = await _handler.HandleAsync(query);

        Assert.False(result.IsSuccess);
        Assert.Equal(RoomErrors.UserNotMember, result.Error);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulRetrievalByMember_ReturnsMappedDetailsWithCode()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var room = Room.Create(hostId, RoomCode.Create("SECRET"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetRoomDetailsQuery(room.Id, memberId);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var details = result.Value;

        Assert.Equal(room.Id, details.Id);
        Assert.Equal("SECRET", details.Code);
        Assert.Equal(RoomStatus.Open, details.Status);
        Assert.Equal(hostId, details.HostUserId);
        Assert.Equal(2, details.Members.Count);
    }

    [Fact]
    public async Task HandleAsync_SuccessfulRetrievalByHost_ReturnsMappedDetailsWithCode()
    {
        var hostId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("HSTCDE"), RoomSettings.Default);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetRoomDetailsQuery(room.Id, hostId);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var details = result.Value;

        Assert.Equal(room.Id, details.Id);
        Assert.Equal("HSTCDE", details.Code);
        Assert.Equal(hostId, details.HostUserId);
        Assert.Single(details.Members);
        Assert.Equal(hostId, details.Members[0].UserId);
        Assert.Equal(RoomRole.Host, details.Members[0].Role);
    }

    [Fact]
    public async Task HandleAsync_CorrectMapping_SettingsStatusHostMemberships()
    {
        var hostId = Guid.NewGuid();
        var member1Id = Guid.NewGuid();
        var member2Id = Guid.NewGuid();

        var customSettings = RoomSettings.Create(maxMembers: 6, allowSpectators: false);

        var room = Room.Create(hostId, RoomCode.Create("MAPTST"), customSettings);
        room.AddMember(member1Id);
        room.AddMember(member2Id);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetRoomDetailsQuery(room.Id, member1Id);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var details = result.Value;

        Assert.Equal(room.Id, details.Id);
        Assert.Equal(RoomStatus.Open, details.Status);
        Assert.Equal(hostId, details.HostUserId);
        Assert.Equal(customSettings, details.Settings);
        Assert.Equal(3, details.Members.Count);

        var hostDetail = Assert.Single(details.Members, m => m.UserId == hostId);
        Assert.Equal(RoomRole.Host, hostDetail.Role);
        Assert.Null(hostDetail.DisplayName);

        var member1Detail = Assert.Single(details.Members, m => m.UserId == member1Id);
        Assert.Equal(RoomRole.Member, member1Detail.Role);
        Assert.Null(member1Detail.DisplayName);

        var member2Detail = Assert.Single(details.Members, m => m.UserId == member2Id);
        Assert.Equal(RoomRole.Member, member2Detail.Role);
        Assert.Null(member2Detail.DisplayName);
    }

    [Fact]
    public async Task HandleAsync_ReturnedReadModelDoesNotExposeAggregateOrMutableCollections()
    {
        var hostId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var room = Room.Create(hostId, RoomCode.Create("RMTTST"), RoomSettings.Default);
        room.AddMember(memberId);

        await _roomRepository.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();

        var query = new GetRoomDetailsQuery(room.Id, memberId);
        var result = await _handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        var details = result.Value;

        // Verify type is IReadOnlyList and casting to mutable IList or List fails or is read-only
        Assert.IsAssignableFrom<IReadOnlyList<RoomMemberDetails>>(details.Members);
        var list = details.Members as IList<RoomMemberDetails>;
        if (list is not null)
        {
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(new RoomMemberDetails(Guid.NewGuid(), RoomRole.Member, DateTimeOffset.UtcNow)));
        }
    }
}
