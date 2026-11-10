namespace Boardy.Domain.Matches;

public sealed class GameDefinitionId : IEquatable<GameDefinitionId>
{
    public string Value { get; }

    private GameDefinitionId(string value)
    {
        Value = value;
    }

    public static GameDefinitionId Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length is < 1 or > 64)
        {
            throw new ArgumentException("GameDefinitionId length must be between 1 and 64 characters.", nameof(value));
        }

        if (!IsAsciiLetterOrDigit(value[0]))
        {
            throw new ArgumentException("GameDefinitionId must begin with a lowercase ASCII letter or digit.", nameof(value));
        }

        if (!IsAsciiLetterOrDigit(value[^1]))
        {
            throw new ArgumentException("GameDefinitionId must end with a lowercase ASCII letter or digit.", nameof(value));
        }

        bool previousWasHyphen = false;
        foreach (char c in value)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                previousWasHyphen = false;
            }
            else if (c is '-')
            {
                if (previousWasHyphen)
                {
                    throw new ArgumentException("GameDefinitionId cannot contain consecutive hyphens.", nameof(value));
                }

                previousWasHyphen = true;
            }
            else
            {
                throw new ArgumentException($"GameDefinitionId contains invalid character '{c}'. Only lowercase ASCII letters, digits, and non-consecutive hyphens are allowed.", nameof(value));
            }
        }

        return new GameDefinitionId(value);
    }

    private static bool IsAsciiLetterOrDigit(char c) =>
        c is >= 'a' and <= 'z' or >= '0' and <= '9';

    public bool Equals(GameDefinitionId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => Equals(obj as GameDefinitionId);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(GameDefinitionId? left, GameDefinitionId? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(GameDefinitionId? left, GameDefinitionId? right) => !(left == right);

    public override string ToString() => Value;
}
