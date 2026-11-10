namespace Boardy.Application.Tests.Identity;

using Boardy.Application.Identity;

public class Sha256RecoveryCredentialHasherTests
{
    private readonly Sha256RecoveryCredentialHasher _hasher = new();

    [Fact]
    public void HashCredential_ReturnsDeterministic64CharLowercaseHexString()
    {
        const string input = "test-recovery-credential";

        var hash1 = _hasher.HashCredential(input);
        var hash2 = _hasher.HashCredential(input);

        Assert.Equal(64, hash1.Length);
        Assert.Equal(hash1, hash2);
        Assert.True(hash1.All(c => char.IsAsciiHexDigitLower(c)));
    }

    [Fact]
    public void HashCredential_MatchesStandardSha256KnownVector()
    {
        // SHA-256("test") = 9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08
        var hash = _hasher.HashCredential("test");

        Assert.Equal("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08", hash);
    }

    [Fact]
    public void HashCredential_ProducesDifferentHashesForDifferentInputs()
    {
        var hashA = _hasher.HashCredential("credential-alpha");
        var hashB = _hasher.HashCredential("credential-beta");

        Assert.NotEqual(hashA, hashB);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public void HashCredential_ThrowsArgumentException_WhenInputIsEmptyOrWhitespace(string invalidInput)
    {
        Assert.Throws<ArgumentException>(() => _hasher.HashCredential(invalidInput));
    }

    [Fact]
    public void HashCredential_ThrowsArgumentNullException_WhenInputIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.HashCredential(null!));
    }
}
