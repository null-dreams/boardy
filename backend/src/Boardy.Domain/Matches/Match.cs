using System.Collections.ObjectModel;

namespace Boardy.Domain.Matches;

public sealed class Match
{
    private readonly ReadOnlyCollection<MatchParticipantSnapshot> _participants;
    private readonly HashSet<Guid> _participantUserIds;

    public Guid Id { get; }

    public GameDefinitionId GameDefinitionId { get; }

    public MatchStatus Status { get; private set; }

    public IReadOnlyCollection<MatchParticipantSnapshot> Participants => _participants;

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? AbortedAt { get; private set; }

    public MatchResult? Result { get; private set; }

    public MatchAbortReason? AbortReason { get; private set; }

    private Match(
        Guid id,
        GameDefinitionId gameDefinitionId,
        ReadOnlyCollection<MatchParticipantSnapshot> participants,
        HashSet<Guid> participantUserIds,
        DateTimeOffset createdAt)
    {
        Id = id;
        GameDefinitionId = gameDefinitionId;
        _participants = participants;
        _participantUserIds = participantUserIds;
        Status = MatchStatus.Pending;
        CreatedAt = createdAt;
    }

    public static Match Create(
        GameDefinitionId gameDefinitionId,
        IEnumerable<MatchParticipantSnapshot> participants)
    {
        ArgumentNullException.ThrowIfNull(gameDefinitionId);
        ArgumentNullException.ThrowIfNull(participants);

        var participantList = new List<MatchParticipantSnapshot>();
        var userIds = new HashSet<Guid>();
        var seatOrders = new HashSet<int>();

        foreach (var participant in participants)
        {
            ArgumentNullException.ThrowIfNull(participant, nameof(participants));

            if (!userIds.Add(participant.UserId))
            {
                throw new ArgumentException($"Duplicate participant user ID '{participant.UserId}'.", nameof(participants));
            }

            if (!seatOrders.Add(participant.SeatOrder))
            {
                throw new ArgumentException($"Duplicate participant seat order '{participant.SeatOrder}'.", nameof(participants));
            }

            participantList.Add(participant);
        }

        if (participantList.Count == 0)
        {
            throw new ArgumentException("A match must have at least one participant.", nameof(participants));
        }

        return new Match(
            Guid.NewGuid(),
            gameDefinitionId,
            participantList.AsReadOnly(),
            userIds,
            DateTimeOffset.UtcNow);
    }

    public void BeginInitialization()
    {
        if (Status != MatchStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot begin initialization when match status is '{Status}'.");
        }

        Status = MatchStatus.Initializing;
    }

    public void Activate()
    {
        if (Status != MatchStatus.Initializing)
        {
            throw new InvalidOperationException($"Cannot activate match when status is '{Status}'.");
        }

        Status = MatchStatus.Active;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete(MatchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (Status != MatchStatus.Active)
        {
            throw new InvalidOperationException($"Cannot complete match when status is '{Status}'.");
        }

        foreach (var winnerId in result.WinnerUserIds)
        {
            if (!_participantUserIds.Contains(winnerId))
            {
                throw new ArgumentException($"Winner user ID '{winnerId}' was not a participant in this match.", nameof(result));
            }
        }

        Status = MatchStatus.Completed;
        Result = result;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Abort(MatchAbortReason reason)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentException($"Invalid match abort reason '{reason}'.", nameof(reason));
        }

        if (Status is MatchStatus.Completed or MatchStatus.Aborted)
        {
            throw new InvalidOperationException($"Cannot abort a match that is already in terminal state '{Status}'.");
        }

        if (Status == MatchStatus.Pending)
        {
            throw new InvalidOperationException("Cannot abort a match before initialization begins.");
        }

        if (Status == MatchStatus.Initializing)
        {
            if (reason != MatchAbortReason.InitializationFailed)
            {
                throw new InvalidOperationException($"Cannot abort an initializing match with reason '{reason}'. Only '{MatchAbortReason.InitializationFailed}' is allowed.");
            }
        }
        else if (Status == MatchStatus.Active)
        {
            if (reason == MatchAbortReason.InitializationFailed)
            {
                throw new InvalidOperationException("Cannot abort an active match with InitializationFailed reason.");
            }
        }
        else
        {
            throw new InvalidOperationException($"Cannot abort match from status '{Status}'.");
        }

        Status = MatchStatus.Aborted;
        AbortReason = reason;
        AbortedAt = DateTimeOffset.UtcNow;
    }
}
