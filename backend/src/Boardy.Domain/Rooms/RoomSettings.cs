namespace Boardy.Domain.Rooms;

public sealed class RoomSettings : IEquatable<RoomSettings>
{
    public const int MinMembers = 2;
    public const int MaxAllowedMembers = 16;
    public const int DefaultMaxMembers = 4;

    public int MaxMembers { get; }
    public bool AllowSpectators { get; }

    public static RoomSettings Default { get; } = new RoomSettings(DefaultMaxMembers, false);

    private RoomSettings(int maxMembers, bool allowSpectators)
    {
        MaxMembers = maxMembers;
        AllowSpectators = allowSpectators;
    }

    public static RoomSettings Create(int maxMembers, bool allowSpectators = false)
    {
        if (maxMembers is < MinMembers or > MaxAllowedMembers)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMembers),
                maxMembers,
                $"MaxMembers must be between {MinMembers} and {MaxAllowedMembers}.");
        }

        return new RoomSettings(maxMembers, allowSpectators);
    }

    public bool Equals(RoomSettings? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return MaxMembers == other.MaxMembers && AllowSpectators == other.AllowSpectators;
    }

    public override bool Equals(object? obj) => Equals(obj as RoomSettings);

    public override int GetHashCode() => HashCode.Combine(MaxMembers, AllowSpectators);

    public static bool operator ==(RoomSettings? left, RoomSettings? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RoomSettings? left, RoomSettings? right) => !(left == right);

    public override string ToString() => $"MaxMembers: {MaxMembers}, AllowSpectators: {AllowSpectators}";
}

