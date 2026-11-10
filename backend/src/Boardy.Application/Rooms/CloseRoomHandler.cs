namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Rooms;

public sealed class CloseRoomHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseRoomHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Room>> HandleAsync(
        CloseRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoomId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotFound);
        }

        if (command.RequesterUserId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.UserNotFound);
        }

        var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotFound);
        }

        if (command.RequesterUserId != room.HostUserId)
        {
            return Result.Failure<Room>(RoomErrors.OnlyHostCanCloseRoom);
        }

        if (room.Status == RoomStatus.Closed)
        {
            return Result.Failure<Room>(RoomErrors.RoomClosed);
        }

        if (room.Status == RoomStatus.StartingMatch || room.Status == RoomStatus.InMatch)
        {
            return Result.Failure<Room>(RoomErrors.CannotCloseDuringMatch);
        }

        room.Close(command.RequesterUserId);

        try
        {
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<Room>(RoomErrors.PersistenceFailed);
        }

        return Result.Success(room);
    }
}
