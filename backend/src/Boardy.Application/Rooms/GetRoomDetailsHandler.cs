namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Results;
using Boardy.Domain.Rooms;

public sealed class GetRoomDetailsHandler
{
    private readonly IRoomRepository _roomRepository;

    public GetRoomDetailsHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
    }

    public async Task<Result<RoomDetails>> HandleAsync(
        GetRoomDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        if (query.RoomId == Guid.Empty)
        {
            return Result.Failure<RoomDetails>(RoomErrors.RoomNotFound);
        }

        if (query.RequesterUserId == Guid.Empty)
        {
            return Result.Failure<RoomDetails>(RoomErrors.UserNotFound);
        }

        var room = await _roomRepository.GetByIdAsync(query.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<RoomDetails>(RoomErrors.RoomNotFound);
        }

        var isMember = room.Memberships.Any(m => m.UserId == query.RequesterUserId);
        if (!isMember)
        {
            return Result.Failure<RoomDetails>(RoomErrors.UserNotMember);
        }

        var memberDetails = room.Memberships
            .Select(m => new RoomMemberDetails(m.UserId, m.Role, m.JoinedAt, DisplayName: null))
            .ToList()
            .AsReadOnly();

        var details = new RoomDetails(
            room.Id,
            room.Status,
            room.HostUserId,
            room.Settings,
            room.Code.Value,
            memberDetails);

        return Result.Success(details);
    }
}
