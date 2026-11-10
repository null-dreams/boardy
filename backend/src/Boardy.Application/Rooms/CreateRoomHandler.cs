namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Application.Identity;
using Boardy.Domain.Rooms;

public sealed class CreateRoomHandler
{
    public const int MaxCodeGenerationAttempts = 5;

    private readonly IRoomRepository _roomRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoomCodeGenerator _roomCodeGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
        : this(roomRepository, userRepository, new CryptographicRoomCodeGenerator(), unitOfWork)
    {
    }

    public CreateRoomHandler(
        IRoomRepository roomRepository,
        IUserRepository userRepository,
        IRoomCodeGenerator roomCodeGenerator,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roomCodeGenerator = roomCodeGenerator ?? throw new ArgumentNullException(nameof(roomCodeGenerator));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<Room>> HandleAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.HostUserId == Guid.Empty)
        {
            return Result.Failure<Room>(RoomErrors.HostUserNotFound);
        }

        var hostUser = await _userRepository.GetByIdAsync(command.HostUserId, cancellationToken);
        if (hostUser is null)
        {
            return Result.Failure<Room>(RoomErrors.HostUserNotFound);
        }

        RoomCode? roomCode = null;
        for (int attempt = 0; attempt < MaxCodeGenerationAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidateCode = _roomCodeGenerator.Generate();
            var existingRoom = await _roomRepository.GetByCodeAsync(candidateCode, cancellationToken);
            if (existingRoom is null || existingRoom.Status == RoomStatus.Closed)
            {
                roomCode = candidateCode;
                break;
            }
        }

        if (roomCode is null)
        {
            return Result.Failure<Room>(RoomErrors.RoomCodeGenerationExhausted);
        }

        var settings = command.Settings ?? RoomSettings.Default;
        var room = Room.Create(command.HostUserId, roomCode, settings);

        try
        {
            await _roomRepository.AddAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<Room>(RoomErrors.PersistenceFailed);
        }

        return Result.Success(room);
    }
}
