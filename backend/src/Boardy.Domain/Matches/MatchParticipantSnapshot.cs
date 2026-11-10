namespace Boardy.Domain.Matches;

public sealed class MatchParticipantSnapshot : IEquatable<MatchParticipantSnapshot>
{
    public Guid UserId { get; }
    public string DisplayName { get; }
    public int SeatOrder { get; }

    private MatchParticipantSnapshot(Guid userId, string displayName, int seatOrder)
    {
        UserId = userId;
        DisplayName = displayName;
        SeatOrder = seatOrder;
    }

    public static MatchParticipantSnapshot Create(Guid userId, string displayName, int seatOrder)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfNegative(seatOrder);

        return new MatchParticipantSnapshot(userId, displayName.Trim(), seatOrder);
    }

    public bool Equals(MatchParticipantSnapshot? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return UserId == other.UserId &&
               string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal) &&
               SeatOrder == other.SeatOrder;
    }

    public override bool Equals(object? obj) => Equals(obj as MatchParticipantSnapshot);

    public override int GetHashCode() =>
        HashCode.Combine(UserId, StringComparer.Ordinal.GetHashCode(DisplayName), SeatOrder);

    public static bool operator ==(MatchParticipantSnapshot? left, MatchParticipantSnapshot? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(MatchParticipantSnapshot? left, MatchParticipantSnapshot? right) => !(left == right);

    public override string ToString() => $"Seat {SeatOrder}: {DisplayName} ({UserId})";
}
