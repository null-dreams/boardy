namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Application.Identity;
using Boardy.Domain.Rooms;

public sealed class JoinRoomHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JoinRoomHandler(
        IRoomRepository roomRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Room>> HandleAsync(
        JoinRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.UserNotFound);
        }

        if (string.IsNullOrWhiteSpace(command.RoomCode))
        {
            return Result.Failure<Room>(RoomErrors.InvalidRoomCode);
        }

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<Room>(RoomErrors.UserNotFound);
        }

        RoomCode roomCode;
        try
        {
            roomCode = RoomCode.Create(command.RoomCode);
        }
        catch (ArgumentException)
        {
            return Result.Failure<Room>(RoomErrors.InvalidRoomCode);
        }

        var room = await _roomRepository.GetByCodeAsync(roomCode, cancellationToken);
        if (room is null)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotFound);
        }

        if (room.Status != RoomStatus.Open)
        {
            return Result.Failure<Room>(RoomErrors.RoomNotOpen);
        }

        try
        {
            room.AddMember(command.UserId);
        }
        catch (InvalidOperationException)
        {
            if (room.Memberships.Any(m => m.UserId == command.UserId))
            {
                return Result.Failure<Room>(RoomErrors.UserAlreadyMember);
            }

            if (room.Memberships.Count >= room.Settings.MaxMembers)
            {
                return Result.Failure<Room>(RoomErrors.RoomFull);
            }

            return Result.Failure<Room>(RoomErrors.CannotJoin);
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
