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

    public override string ToString() => $"UserId: {UserId}, Role: {Role}, Status: {Status}, JoinedAt: {JoinedAt:O}";
}
