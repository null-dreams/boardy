using System.Reflection;
using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class GameDefinitionIdTests
{
    [Theory]
    [InlineData("catan")]
    [InlineData("chess")]
    [InlineData("uno")]
    [InlineData("ticket-to-ride")]
    [InlineData("game-2048")]
    [InlineData("a")]
    [InlineData("9")]
    [InlineData("1-2-3")]
    public void Create_WithValidIdentifier_Succeeds(string validId)
    {
        // Act
        var gameDefId = GameDefinitionId.Create(validId);

        // Assert
        Assert.Equal(validId, gameDefId.Value);
    }

    [Fact]
    public void Create_WithBoundaryLengths_Succeeds()
    {
        // Boundary: 1 character
        var minLength = GameDefinitionId.Create("a");
        Assert.Equal("a", minLength.Value);

        // Boundary: 64 characters
        var exact64 = new string('a', 64);
        var maxLenId = GameDefinitionId.Create(exact64);
        Assert.Equal(exact64, maxLenId.Value);
    }

    [Fact]
    public void Create_BeyondMaximumLength_ThrowsArgumentException()
    {
        // 65 characters exceeds max length of 64
        var tooLong = new string('a', 65);
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(tooLong));
    }

    [Fact]
    public void Create_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GameDefinitionId.Create(null!));
    }

    [Fact]
    public void Create_WithEmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(string.Empty));
    }

    [Theory]
    [InlineData("Catan")]
    [InlineData("cAtan")]
    [InlineData("CATAN")]
    [InlineData("Ticket-To-Ride")]
    [InlineData("GAME")]
    public void Create_WithUppercaseCharacters_ThrowsArgumentException(string upperId)
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(upperId));
    }

    [Theory]
    [InlineData("-catan")]
    [InlineData("catan-")]
    [InlineData("-catan-")]
    [InlineData("-")]
    public void Create_WithLeadingOrTrailingHyphen_ThrowsArgumentException(string invalidId)
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(invalidId));
    }

    [Theory]
    [InlineData("ticket--to-ride")]
    [InlineData("a---b")]
    [InlineData("catan--")]
    [InlineData("--catan")]
    public void Create_WithConsecutiveHyphens_ThrowsArgumentException(string consecutiveHyphenId)
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(consecutiveHyphenId));
    }

    [Theory]
    [InlineData(" Catan")]
    [InlineData("catan ")]
    [InlineData("catan game")]
    [InlineData("catan\tgame")]
    [InlineData("game_name")]
    [InlineData("game.name")]
    [InlineData("catan!")]
    [InlineData("catan#1")]
    public void Create_WithWhitespaceOrPunctuation_ThrowsArgumentException(string invalidId)
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(invalidId));
    }

    [Theory]
    [InlineData("cátan")]
    [InlineData("über-game")]
    [InlineData("ｃａｔａｎ")]
    [InlineData("１２３４")]
    public void Create_WithNonAsciiCharacters_ThrowsArgumentException(string nonAsciiId)
    {
        Assert.Throws<ArgumentException>(() => GameDefinitionId.Create(nonAsciiId));
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnTrueForSameValue()
    {
        // Arrange
        var id1 = GameDefinitionId.Create("catan");
        var id2 = GameDefinitionId.Create("catan");

        // Assert
        Assert.True(id1.Equals(id2));
        Assert.True(id1.Equals((object)id2));
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnFalseForDifferentValuesOrNull()
    {
        // Arrange
        var id1 = GameDefinitionId.Create("catan");
        var id2 = GameDefinitionId.Create("chess");

        // Assert
        Assert.False(id1.Equals(id2));
        Assert.False(id1.Equals((object)id2));
        Assert.False(id1 == id2);
        Assert.True(id1 != id2);

        Assert.False(id1.Equals(null));
        Assert.False(id1.Equals((object?)null));
        Assert.False(id1.Equals("catan"));
        Assert.False(id1 == null);
        Assert.False(null == id1);
        Assert.True(id1 != null);
        Assert.True(null != id1);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashCodeForEqualInstances()
    {
        // Arrange
        var id1 = GameDefinitionId.Create("ticket-to-ride");
        var id2 = GameDefinitionId.Create("ticket-to-ride");

        // Assert
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var id = GameDefinitionId.Create("catan");

        // Act & Assert
        Assert.Equal("catan", id.ToString());
    }

    [Fact]
    public void PublicProperties_AreImmutable()
    {
        // Arrange
        var property = typeof(GameDefinitionId).GetProperty(nameof(GameDefinitionId.Value));

        // Assert
        Assert.NotNull(property);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite, "Value property should not have a setter.");
    }
}
