using Boardy.Domain.Rooms;

namespace Boardy.Domain.Tests.Rooms;

public class RoomCodeTests
{
    [Theory]
    [InlineData("ABC234", "ABC234")]
    [InlineData("abc234", "ABC234")]
    [InlineData("aBc234", "ABC234")]
    [InlineData("ABC2345", "ABC2345")]
    [InlineData("abc2345", "ABC2345")]
    [InlineData("ABC23456", "ABC23456")]
    [InlineData("abc23456", "ABC23456")]
    [InlineData("23456789", "23456789")]
    [InlineData("JKMNPQR", "JKMNPQR")]
    public void Create_WithValidCode_NormalizesToUppercase(string input, string expected)
    {
        // Act
        var roomCode = RoomCode.Create(input);

        // Assert
        Assert.Equal(expected, roomCode.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AB")]
    [InlineData("ABC")]
    [InlineData("ABCD")]
    [InlineData("ABC23")]
    [InlineData("ABC234567")]
    [InlineData("ABC2345678")]
    public void Create_WithLengthOutsideValidRange_ThrowsArgumentException(string invalidLengthCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomCode.Create(invalidLengthCode));
    }

    [Theory]
    [InlineData("ABCD0E")]
    [InlineData("ABCD1E")]
    [InlineData("ABCDOE")]
    [InlineData("ABCDoE")]
    [InlineData("ABCDIE")]
    [InlineData("ABCDiE")]
    [InlineData("0ABCDE")]
    [InlineData("1ABCDE")]
    [InlineData("OABCDE")]
    [InlineData("oABCDE")]
    [InlineData("IABCDE")]
    [InlineData("iABCDE")]
    public void Create_WithAmbiguousCharacters_ThrowsArgumentException(string ambiguousCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomCode.Create(ambiguousCode));
    }

    [Theory]
    [InlineData("ABC-DEF")]
    [InlineData("ABC_DEF")]
    [InlineData("ABC.DEF")]
    [InlineData("ABC!DEF")]
    [InlineData(" ABCDEF")]
    [InlineData("ABCDEF ")]
    [InlineData("ABC DEF")]
    [InlineData("      ")]
    [InlineData("        ")]
    public void Create_WithInvalidCharactersOrWhitespace_ThrowsArgumentException(string invalidCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomCode.Create(invalidCode));
    }

    [Theory]
    [InlineData("ÄBCDEF")]
    [InlineData("ébcdef")]
    [InlineData("ＡＢＣＤＥＦ")]
    [InlineData("１２３４５６")]
    public void Create_WithNonAsciiOrFullWidthCharacters_ThrowsArgumentException(string nonAsciiCode)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => RoomCode.Create(nonAsciiCode));
    }

    [Fact]
    public void Create_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => RoomCode.Create(null!));
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnTrueForSameNormalizedValue()
    {
        // Arrange
        var code1 = RoomCode.Create("abc234");
        var code2 = RoomCode.Create("ABC234");

        // Assert
        Assert.True(code1.Equals(code2));
        Assert.True(code1.Equals((object)code2));
        Assert.True(code1 == code2);
        Assert.False(code1 != code2);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnFalseForDifferentValuesOrNull()
    {
        // Arrange
        var code1 = RoomCode.Create("ABC234");
        var code2 = RoomCode.Create("XYZ789");

        // Assert
        Assert.False(code1.Equals(code2));
        Assert.False(code1.Equals((object)code2));
        Assert.False(code1 == code2);
        Assert.True(code1 != code2);

        Assert.False(code1.Equals(null));
        Assert.False(code1.Equals((object?)null));
        Assert.False(code1.Equals("ABC234"));
        Assert.False(code1 == null);
        Assert.False(null == code1);
        Assert.True(code1 != null);
        Assert.True(null != code1);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashCodeForEqualInstances()
    {
        // Arrange
        var code1 = RoomCode.Create("abc234");
        var code2 = RoomCode.Create("ABC234");

        // Assert
        Assert.Equal(code1.GetHashCode(), code2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        // Arrange
        var code = RoomCode.Create("abc234");

        // Act & Assert
        Assert.Equal("ABC234", code.ToString());
    }
}
