namespace Boardy.Application.Matches;

using Boardy.Application.Common.Persistence;
using Boardy.Application.Common.Results;
using Boardy.Application.Identity;
using Boardy.Application.Rooms;
using Boardy.Domain.Matches;
using Boardy.Domain.Rooms;

/// <summary>
/// Handles match creation, initialization, and activation.
/// Cross-aggregate mutations on both Match and Room are coordinated and committed
/// atomically through a single IUnitOfWork invocation. Concurrent start requests
/// rely on the room's persisted lifecycle state and optimistic concurrency tokens
/// enforced at the infrastructure level.
/// </summary>
public sealed class StartMatchHandler
{
    private readonly IRoomRepository _roomRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public StartMatchHandler(
        IRoomRepository roomRepository,
        IMatchRepository matchRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _roomRepository = roomRepository ?? throw new ArgumentNullException(nameof(roomRepository));
        _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result<StartMatchResult>> HandleAsync(
        StartMatchCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoomId == Guid.Empty)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.RoomNotFound);
        }

        if (command.HostUserId == Guid.Empty)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.UserNotFound);
        }

        if (string.IsNullOrWhiteSpace(command.GameDefinitionId))
        {
            return Result.Failure<StartMatchResult>(MatchErrors.InvalidGameDefinitionId);
        }

        GameDefinitionId gameDefinitionId;
        try
        {
            gameDefinitionId = GameDefinitionId.Create(command.GameDefinitionId);
        }
        catch (ArgumentException)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.InvalidGameDefinitionId);
        }

        var room = await _roomRepository.GetByIdAsync(command.RoomId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.RoomNotFound);
        }

        if (command.HostUserId != room.HostUserId)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.OnlyHostCanStartMatch);
        }

        if (room.Status != RoomStatus.Open)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.RoomNotOpen);
        }

        var orderedMembers = room.Memberships
            .OrderBy(m => m.JoinedAt)
            .ThenBy(m => m.UserId)
            .ToList();

        var snapshots = new List<MatchParticipantSnapshot>(orderedMembers.Count);
        for (int seatOrder = 0; seatOrder < orderedMembers.Count; seatOrder++)
        {
            var member = orderedMembers[seatOrder];
            var user = await _userRepository.GetByIdAsync(member.UserId, cancellationToken);
            if (user is null)
            {
                return Result.Failure<StartMatchResult>(MatchErrors.ParticipantUserNotFound);
            }

            snapshots.Add(MatchParticipantSnapshot.Create(member.UserId, user.DisplayName, seatOrder));
        }

        var match = Match.Create(gameDefinitionId, snapshots);
        room.BeginMatch(command.HostUserId, match.Id);
        match.BeginInitialization();
        match.Activate();
        room.MarkMatchInProgress();

        try
        {
            await _matchRepository.AddAsync(match, cancellationToken);
            await _roomRepository.UpdateAsync(room, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception)
        {
            return Result.Failure<StartMatchResult>(MatchErrors.PersistenceFailed);
        }

        var result = new StartMatchResult(
            match.Id,
            room.Id,
            gameDefinitionId.Value,
            match.Status,
            match.Participants.ToList().AsReadOnly());

        return Result.Success(result);
    }
}
