namespace Boardy.Domain.Rooms;

public sealed class RoomCode : IEquatable<RoomCode>
{
    public string Value { get; }

    private RoomCode(string value)
    {
        Value = value;
    }

    public static RoomCode Create(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code.Length is < 6 or > 8)
        {
            throw new ArgumentException("Room code must be between 6 and 8 characters in length.", nameof(code));
        }

        foreach (char c in code)
        {
            if (c is '0' or '1' or 'O' or 'o' or 'I' or 'i')
            {
                throw new ArgumentException($"Room code cannot contain ambiguous character '{c}'.", nameof(code));
            }

            if (!((c is >= 'A' and <= 'Z') || (c is >= 'a' and <= 'z') || (c is >= '2' and <= '9')))
            {
                throw new ArgumentException($"Room code contains invalid character '{c}'. Only alphanumeric characters are allowed.", nameof(code));
            }
        }

        return new RoomCode(code.ToUpperInvariant());
    }

    public bool Equals(RoomCode? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as RoomCode);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(RoomCode? left, RoomCode? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(RoomCode? left, RoomCode? right) => !(left == right);

    public override string ToString() => Value;
}
