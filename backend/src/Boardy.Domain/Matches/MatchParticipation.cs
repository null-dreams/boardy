namespace Boardy.Domain.Matches;

public sealed class MatchParticipation
{
    public Guid UserId { get; }
    public MatchRole Role { get; }
    public ParticipationStatus? Status { get; private set; }
    public DateTimeOffset JoinedAt { get; }

    private MatchParticipation(Guid userId, MatchRole role, ParticipationStatus? status, DateTimeOffset joinedAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentException($"Invalid match role: {role}.", nameof(role));
        }

        if (role == MatchRole.Spectator && status is not null)
        {
            throw new ArgumentException("Spectators cannot have a participation status.", nameof(status));
        }

        if (role == MatchRole.Participant && status is null)
        {
            throw new ArgumentException("Participants must have a participation status.", nameof(status));
        }

        if (status is not null && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentException($"Invalid participation status: {status}.", nameof(status));
        }

        UserId = userId;
        Role = role;
        Status = status;
        JoinedAt = joinedAt;
    }

    public static MatchParticipation CreateParticipant(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return new MatchParticipation(userId, MatchRole.Participant, ParticipationStatus.Active, DateTimeOffset.UtcNow);
    }

    public static MatchParticipation CreateSpectator(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return new MatchParticipation(userId, MatchRole.Spectator, null, DateTimeOffset.UtcNow);
    }

    internal void Disconnect()
    {
        if (Role != MatchRole.Participant || Status is null)
        {
            throw new InvalidOperationException("Spectators do not have a participation status.");
        }

        if (Status == ParticipationStatus.Disconnected)
        {
            throw new InvalidOperationException("Participant is already disconnected.");
        }

        if (Status == ParticipationStatus.Forfeited)
        {
            throw new InvalidOperationException("Cannot disconnect a forfeited participant.");
        }

        if (Status != ParticipationStatus.Active)
        {
            throw new InvalidOperationException($"Cannot disconnect participant with status '{Status}'.");
        }

        Status = ParticipationStatus.Disconnected;
    }

    internal void Reconnect()
    {
        if (Role != MatchRole.Participant || Status is null)
        {
            throw new InvalidOperationException("Spectators do not have a participation status.");
        }

        if (Status == ParticipationStatus.Active)
        {
            throw new InvalidOperationException("Participant is already active.");
        }

        if (Status == ParticipationStatus.Forfeited)
        {
            throw new InvalidOperationException("A forfeited participant cannot reconnect.");
        }

        if (Status != ParticipationStatus.Disconnected)
        {
            throw new InvalidOperationException($"Cannot reconnect participant with status '{Status}'.");
        }

        Status = ParticipationStatus.Active;
    }

    internal void Forfeit()
    {
        if (Role != MatchRole.Participant || Status is null)
        {
            throw new InvalidOperationException("Spectators do not have a participation status.");
        }

        if (Status == ParticipationStatus.Forfeited)
        {
            throw new InvalidOperationException("Participant has already forfeited.");
        }

        if (Status != ParticipationStatus.Active && Status != ParticipationStatus.Disconnected)
        {
            throw new InvalidOperationException($"Cannot forfeit participant with status '{Status}'.");
        }

        Status = ParticipationStatus.Forfeited;
    }

    public override string ToString() => $"UserId: {UserId}, Role: {Role}, Status: {Status}, JoinedAt: {JoinedAt:O}";
}
