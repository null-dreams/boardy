namespace Boardy.Application.Tests.Rooms;

using Boardy.Application.Rooms;

public class CryptographicRoomCodeGeneratorTests
{
    private readonly CryptographicRoomCodeGenerator _generator = new();

    [Fact]
    public void Generate_ProducesValidRoomCodeWithDefaultLengthOf6()
    {
        var code = _generator.Generate();

        Assert.NotNull(code);
        Assert.Equal(6, code.Value.Length);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void Constructor_AllowsValidLengths(int length)
    {
        var generator = new CryptographicRoomCodeGenerator(length);
        var code = generator.Generate();

        Assert.Equal(length, code.Value.Length);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ThrowsArgumentOutOfRangeException_ForInvalidLengths(int invalidLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CryptographicRoomCodeGenerator(invalidLength));
    }

    [Fact]
    public void Generate_ExcludesAmbiguousCharacters()
    {
        const int iterations = 100;
        char[] ambiguousChars = ['0', 'O', '1', 'I'];

        for (int i = 0; i < iterations; i++)
        {
            var code = _generator.Generate();
            foreach (char c in ambiguousChars)
            {
                Assert.DoesNotContain(c, code.Value);
            }
        }
    }

    [Fact]
    public void Generate_ProducesDiverseCodesAcrossMultipleCalls()
    {
        const int iterations = 100;
        var uniqueCodes = new HashSet<string>();

        for (int i = 0; i < iterations; i++)
        {
            var code = _generator.Generate();
            Assert.True(uniqueCodes.Add(code.Value), "Generated room codes should be unique across 100 iterations.");
        }
    }
}
