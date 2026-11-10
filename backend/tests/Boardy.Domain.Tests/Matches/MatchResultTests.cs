using System.Reflection;
using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchResultTests
{
    [Fact]
    public void Create_WithSingleWinner_SuccessfullyCreatesResult()
    {
        // Arrange
        var winnerId = Guid.NewGuid();

        // Act
        var result = MatchResult.Create([winnerId]);

        // Assert
        Assert.False(result.IsDraw);
        Assert.Single(result.WinnerUserIds);
        Assert.Contains(winnerId, result.WinnerUserIds);
    }

    [Fact]
    public void Create_WithMultipleWinners_SuccessfullyCreatesResultAndPreservesOrdering()
    {
        // Arrange
        var winner1 = Guid.NewGuid();
        var winner2 = Guid.NewGuid();
        var winner3 = Guid.NewGuid();
        var winners = new[] { winner1, winner2, winner3 };

        // Act
        var result = MatchResult.Create(winners);

        // Assert
        Assert.False(result.IsDraw);
        Assert.Equal(3, result.WinnerUserIds.Count);
        Assert.Equal(winners, result.WinnerUserIds);
    }

    [Fact]
    public void Create_WithEmptyCollection_RepresentsDraw()
    {
        // Act
        var result = MatchResult.Create(Array.Empty<Guid>());

        // Assert
        Assert.True(result.IsDraw);
        Assert.Empty(result.WinnerUserIds);
    }

    [Fact]
    public void Create_WithNullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MatchResult.Create(null!));
    }

    [Fact]
    public void Create_WithEmptyGuidInWinners_ThrowsArgumentException()
    {
        // Arrange
        var validId = Guid.NewGuid();
        var invalidWinners = new[] { validId, Guid.Empty };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MatchResult.Create(invalidWinners));
    }

    [Fact]
    public void Create_WithDuplicateWinnerIds_ThrowsArgumentException()
    {
        // Arrange
        var winnerId = Guid.NewGuid();
        var duplicateWinners = new[] { winnerId, winnerId };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => MatchResult.Create(duplicateWinners));
    }

    [Fact]
    public void Create_MaterializesInputSequence_PreventingExternalModification()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var inputList = new List<Guid> { id1 };

        // Act
        var result = MatchResult.Create(inputList);
        inputList.Add(id2);

        // Assert
        Assert.Single(result.WinnerUserIds);
        Assert.Contains(id1, result.WinnerUserIds);
        Assert.DoesNotContain(id2, result.WinnerUserIds);
    }

    [Fact]
    public void WinnerUserIds_CannotBeExternallyModified()
    {
        // Arrange
        var winnerId = Guid.NewGuid();
        var result = MatchResult.Create([winnerId]);

        // Act & Assert
        if (result.WinnerUserIds is IList<Guid> list)
        {
            Assert.Throws<NotSupportedException>(() => list.Add(Guid.NewGuid()));
        }
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnTrueForSameSequences()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var result1 = MatchResult.Create([id1, id2]);
        var result2 = MatchResult.Create([id1, id2]);

        // Assert
        Assert.True(result1.Equals(result2));
        Assert.True(result1.Equals((object)result2));
        Assert.True(result1 == result2);
        Assert.False(result1 != result2);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnFalseForDifferentValuesOrOrder()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var result1 = MatchResult.Create([id1, id2]);
        var resultReversed = MatchResult.Create([id2, id1]);
        var resultDifferent = MatchResult.Create([id1, id3]);
        var drawResult = MatchResult.Create(Array.Empty<Guid>());

        // Assert: Order sensitivity
        Assert.False(result1.Equals(resultReversed));
        Assert.False(result1 == resultReversed);
        Assert.True(result1 != resultReversed);

        // Assert: Different winners
        Assert.False(result1.Equals(resultDifferent));
        Assert.False(result1 == resultDifferent);

        // Assert: Draw vs winners
        Assert.False(result1.Equals(drawResult));

        // Assert: Null and other types
        Assert.False(result1.Equals(null));
        Assert.False(result1.Equals((object?)null));
        Assert.False(result1.Equals("SomeString"));
        Assert.False(result1 == null);
        Assert.False(null == result1);
        Assert.True(result1 != null);
        Assert.True(null != result1);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashCodeForEqualInstances()
    {
        // Arrange
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var result1 = MatchResult.Create([id1, id2]);
        var result2 = MatchResult.Create([id1, id2]);

        // Assert
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsInformativeString()
    {
        // Draw
        var draw = MatchResult.Create(Array.Empty<Guid>());
        Assert.Equal("Draw", draw.ToString());

        // Single winner
        var winnerId = Guid.NewGuid();
        var single = MatchResult.Create([winnerId]);
        Assert.Equal($"Winner: {winnerId}", single.ToString());

        // Multiple winners
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var multiple = MatchResult.Create([id1, id2]);
        Assert.Equal($"Winners: [{id1}, {id2}]", multiple.ToString());
    }

    [Fact]
    public void PublicProperties_AreImmutable()
    {
        // Arrange
        var winnersProp = typeof(MatchResult).GetProperty(nameof(MatchResult.WinnerUserIds));
        var isDrawProp = typeof(MatchResult).GetProperty(nameof(MatchResult.IsDraw));

        // Assert
        Assert.NotNull(winnersProp);
        Assert.True(winnersProp.CanRead);
        Assert.False(winnersProp.CanWrite, "WinnerUserIds property should not have a setter.");

        Assert.NotNull(isDrawProp);
        Assert.True(isDrawProp.CanRead);
        Assert.False(isDrawProp.CanWrite, "IsDraw property should not have a setter.");
    }
}
