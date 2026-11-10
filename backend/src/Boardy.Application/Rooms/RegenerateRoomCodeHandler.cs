namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Domain.Rooms;

public sealed class RegenerateRoomCodeHandler
{
    public const int MaxCodeGenerationAttempts = 5;

    private readonly IRoomRepository _roomRepository;
    private readonly IRoomCodeGenerator _roomCodeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateRoomCodeHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork)
        : this(roomRepository, new CryptographicRoomCodeGenerator(), unitOfWork)
    {
    }

    public RegenerateRoomCodeHandler(
        IRoomRepository roomRepository,
        IRoomCodeGenerator roomCodeGenerator,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _roomCodeGenerator = roomCodeGenerator ?? throw new ArgumentNullException(nameof(roomCodeGenerator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<RoomCode>> HandleAsync(
        RegenerateRoomCodeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoomId == Guid.Empty)
        {
            return Result.Failure<RoomCode>(RoomErrors.RoomNotFound);
        }

        if (command.RequesterUserId == Guid.Empty)
        {
            return Result.Failure<RoomCode>(RoomErrors.UserNotFound);
        }

        var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<RoomCode>(RoomErrors.RoomNotFound);
        }

        if (room.Status == RoomStatus.Closed)
        {
            return Result.Failure<RoomCode>(RoomErrors.RoomClosed);
        }

        if (command.RequesterUserId != room.HostUserId)
        {
            return Result.Failure<RoomCode>(RoomErrors.OnlyHostCanRegenerateCode);
        }

        RoomCode? newRoomCode = null;
        for (int attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidateCode = _roomCodeGenerator.Generate();
            if (candidateCode == room.Code)
            {
                continue;
            }

            var existingRoom = await _roomRepository.GetByCodeAsync(candidateCode, cancellationToken);
            if (existingRoom is null || existingRoom.Status == RoomStatus.Closed)
            {
                newRoomCode = candidateCode;
                break;
            }
        }

        if (newRoomCode is null)
        {
            return Result.Failure<RoomCode>(RoomErrors.RoomCodeGenerationExhausted);
        }

        room.RegenerateCode(newRoomCode);

        try
        {
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<RoomCode>(RoomErrors.PersistenceFailed);
        }

        return Result.Success(newRoomCode);
    }
}
