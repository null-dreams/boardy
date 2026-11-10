namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Rooms;

public sealed class TransferHostHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;

    public TransferHostHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Room>> HandleAsync(
        TransferHostCommand command,
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

        if (command.NewHostUserId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.ProposedHostNotMember);
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

        if (command.RequesterUserId != room.HostUserId)
        {
            return Result.Failure<Room>(RoomErrors.OnlyHostCanTransferHost);
        }

        if (command.NewHostUserId == room.HostUserId)
        {
            return Result.Failure<Room>(RoomErrors.CannotTransferHostToSelf);
        }

        if (!room.Memberships.Any(m => m.UserId == command.NewHostUserId))
        {
            return Result.Failure<Room>(RoomErrors.ProposedHostNotMember);
        }

        room.TransferHost(command.NewHostUserId);

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
