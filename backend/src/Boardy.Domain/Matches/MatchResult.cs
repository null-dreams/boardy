using System.Collections.ObjectModel;

namespace Boardy.Domain.Matches;

public sealed class MatchResult : IEquatable<MatchResult>
{
    private readonly ReadOnlyCollection<Guid> _winnerUserIds;

    public IReadOnlyCollection<Guid> WinnerUserIds => _winnerUserIds;

    public bool IsDraw => _winnerUserIds.Count == 0;

    private MatchResult(ReadOnlyCollection<Guid> winnerUserIds)
    {
        _winnerUserIds = winnerUserIds;
    }

    public static MatchResult Create(IEnumerable<Guid> winnerUserIds)
    {
        ArgumentNullException.ThrowIfNull(winnerUserIds);

        var list = new List<Guid>();
        var seen = new HashSet<Guid>();

        foreach (var id in winnerUserIds)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Winner user ID cannot be empty.", nameof(winnerUserIds));
            }

            if (!seen.Add(id))
            {
                throw new ArgumentException($"Duplicate winner user ID '{id}' is not allowed.", nameof(winnerUserIds));
            }

            list.Add(id);
        }

        return new MatchResult(list.AsReadOnly());
    }

    public bool Equals(MatchResult? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return _winnerUserIds.SequenceEqual(other._winnerUserIds);
    }

    public override bool Equals(object? obj) => Equals(obj as MatchResult);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var id in _winnerUserIds)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(MatchResult? left, MatchResult? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(MatchResult? left, MatchResult? right) => !(left == right);

    public override string ToString()
    {
        if (IsDraw)
        {
            return "Draw";
        }

        if (_winnerUserIds.Count == 1)
        {
            return $"Winner: {_winnerUserIds[0]}";
        }

        return $"Winners: [{string.Join(", ", _winnerUserIds)}]";
    }
}
