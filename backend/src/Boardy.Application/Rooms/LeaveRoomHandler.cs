namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Rooms;

public sealed class LeaveRoomHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LeaveRoomHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Room>> HandleAsync(
        LeaveRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoomId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotFound);
        }

        if (command.UserId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.UserNotFound);
        }

        var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotFound);
        }

        if (room.Status == RoomStatus.Closed)
        {
            return Result.Failure<Room>(RoomErrors.RoomClosed);
        }

        if (room.Status == RoomStatus.StartingMatch || room.Status == RoomStatus.InMatch)
        {
            return Result.Failure<Room>(RoomErrors.CannotLeaveDuringMatch);
        }

        var isMember = room.Memberships.Any(m => m.UserId == command.UserId);
        if (!isMember)
        {
            return Result.Failure<Room>(RoomErrors.UserNotMember);
        }

        if (command.UserId == room.HostUserId)
        {
            if (room.Memberships.Count == 1)
            {
                if (room.Status != RoomStatus.Open)
                {
                    return Result.Failure<Room>(RoomErrors.CannotCloseRoom);
                }

                room.Close(command.UserId);
            }
            else
            {
                if (command.NewHostUserId is null)
                {
                    return Result.Failure<Room>(RoomErrors.HostCannotLeaveWithoutTransfer);
                }

                var newHostUserId = command.NewHostUserId.Value;

                if (newHostUserId == command.UserId)
                {
                    return Result.Failure<Room>(RoomErrors.CannotTransferHostToSelf);
                }

                if (!room.Memberships.Any(m => m.UserId == newHostUserId))
                {
                    return Result.Failure<Room>(RoomErrors.ProposedHostNotMember);
                }

                room.TransferHost(newHostUserId);
                room.RemoveMember(command.UserId);
            }
        }
        else
        {
            room.RemoveMember(command.UserId);
        }

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
