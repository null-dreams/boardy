using System.Collections.ObjectModel;
using Boardy.Domain.Events;
using Boardy.Domain.Matches.Events;

namespace Boardy.Domain.Matches;

public sealed class Match
{
    private readonly ReadOnlyCollection<MatchParticipantSnapshot> _participants;
    private readonly HashSet<Guid> _participantUserIds;
    private readonly List<MatchParticipation> _participantMemberships;
    private readonly List<MatchParticipation> _spectatorMemberships = [];
    private readonly ReadOnlyCollection<MatchParticipation> _readOnlyParticipantMemberships;
    private readonly ReadOnlyCollection<MatchParticipation> _readOnlySpectatorMemberships;
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly ReadOnlyCollection<IDomainEvent> _readOnlyDomainEvents;

    public Guid Id { get; }

    public GameDefinitionId GameDefinitionId { get; }

    public MatchStatus Status { get; private set; }

    public IReadOnlyCollection<MatchParticipantSnapshot> Participants => _participants;

    public IReadOnlyCollection<MatchParticipation> ParticipantMemberships => _readOnlyParticipantMemberships;

    public IReadOnlyCollection<MatchParticipation> SpectatorMemberships => _readOnlySpectatorMemberships;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _readOnlyDomainEvents;

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
        List<MatchParticipation> participantMemberships,
        DateTimeOffset createdAt)
    {
        Id = id;
        GameDefinitionId = gameDefinitionId;
        _participants = participants;
        _participantUserIds = participantUserIds;
        _participantMemberships = participantMemberships;
        _readOnlyParticipantMemberships = _participantMemberships.AsReadOnly();
        _readOnlySpectatorMemberships = _spectatorMemberships.AsReadOnly();
        _readOnlyDomainEvents = _domainEvents.AsReadOnly();
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
        var participantMemberships = new List<MatchParticipation>();
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
            participantMemberships.Add(MatchParticipation.CreateParticipant(participant.UserId));
        }

        if (participantList.Count == 0)
        {
            throw new ArgumentException("A match must have at least one participant.", nameof(participants));
        }

        var match = new Match(
            Guid.NewGuid(),
            gameDefinitionId,
            participantList.AsReadOnly(),
            userIds,
            participantMemberships,
            DateTimeOffset.UtcNow);

        match.RecordEvent(new MatchCreatedEvent(match.Id, gameDefinitionId));
        return match;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void RecordEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public MatchParticipation AddSpectator(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status is MatchStatus.Completed or MatchStatus.Aborted)
        {
            throw new InvalidOperationException($"Cannot add spectator when match status is '{Status}'.");
        }

        if (_participantUserIds.Contains(userId))
        {
            throw new InvalidOperationException($"User '{userId}' is in the participant roster and cannot be admitted as a spectator.");
        }

        if (_spectatorMemberships.Any(s => s.UserId == userId))
        {
            throw new InvalidOperationException($"User '{userId}' is already a spectator in this match.");
        }

        var spectator = MatchParticipation.CreateSpectator(userId);
        _spectatorMemberships.Add(spectator);
        RecordEvent(new MatchSpectatorJoinedEvent(Id, userId));
        return spectator;
    }

    public void RemoveSpectator(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status is MatchStatus.Completed or MatchStatus.Aborted)
        {
            throw new InvalidOperationException($"Cannot remove spectator when match status is '{Status}'.");
        }

        var spectator = _spectatorMemberships.FirstOrDefault(s => s.UserId == userId);
        if (spectator is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a spectator in this match.");
        }

        _spectatorMemberships.Remove(spectator);
        RecordEvent(new MatchSpectatorLeftEvent(Id, userId));
    }

    public void DisconnectParticipant(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status != MatchStatus.Active)
        {
            throw new InvalidOperationException($"Cannot disconnect participant when match status is '{Status}'.");
        }

        var participant = _participantMemberships.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a participant in this match.");
        }

        participant.Disconnect();
        RecordEvent(new MatchParticipantDisconnectedEvent(Id, userId));
    }

    public void ReconnectParticipant(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status != MatchStatus.Active)
        {
            throw new InvalidOperationException($"Cannot reconnect participant when match status is '{Status}'.");
        }

        var participant = _participantMemberships.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a participant in this match.");
        }

        participant.Reconnect();
        RecordEvent(new MatchParticipantReconnectedEvent(Id, userId));
    }

    public void ForfeitParticipant(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (Status != MatchStatus.Active)
        {
            throw new InvalidOperationException($"Cannot forfeit participant when match status is '{Status}'.");
        }

        var participant = _participantMemberships.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
        {
            throw new InvalidOperationException($"User '{userId}' is not a participant in this match.");
        }

        participant.Forfeit();
        RecordEvent(new MatchParticipantForfeitedEvent(Id, userId));
    }

    public void BeginInitialization()
    {
        if (Status != MatchStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot begin initialization when match status is '{Status}'.");
        }

        Status = MatchStatus.Initializing;
        RecordEvent(new MatchInitializationStartedEvent(Id));
    }

    public void Activate()
    {
        if (Status != MatchStatus.Initializing)
        {
            throw new InvalidOperationException($"Cannot activate match when status is '{Status}'.");
        }

        Status = MatchStatus.Active;
        StartedAt = DateTimeOffset.UtcNow;
        RecordEvent(new MatchStartedEvent(Id));
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
        RecordEvent(new MatchCompletedEvent(Id, result));
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
        RecordEvent(new MatchAbortedEvent(Id, reason));
    }
}
