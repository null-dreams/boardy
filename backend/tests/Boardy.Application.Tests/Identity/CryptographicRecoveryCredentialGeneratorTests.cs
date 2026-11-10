namespace Boardy.Application.Tests.Identity;

using Boardy.Application.Identity;

public class CryptographicRecoveryCredentialGeneratorTests
{
    private readonly CryptographicRecoveryCredentialGenerator _generator = new();

    [Fact]
    public void GenerateCredential_ReturnsNonEmptyString()
    {
        var credential = _generator.GenerateCredential();

        Assert.False(string.IsNullOrWhiteSpace(credential));
    }

    [Fact]
    public void GenerateCredential_ReturnsExpectedBase64UrlLengthFor32Bytes()
    {
        // 32 bytes encoded in Base64Url (without padding) is 43 characters
        var credential = _generator.GenerateCredential();

        Assert.Equal(43, credential.Length);
    }

    [Fact]
    public void GenerateCredential_ProducesUniqueValuesAcrossCalls()
    {
        const int iterations = 100;
        var credentials = new HashSet<string>();

        for (int i = 0; i < iterations; i++)
        {
            var credential = _generator.GenerateCredential();
            Assert.True(credentials.Add(credential), "Generated credentials must be unique.");
        }
    }

    [Fact]
    public void GenerateCredential_ContainsOnlyValidBase64UrlCharacters()
    {
        var credential = _generator.GenerateCredential();

        foreach (char c in credential)
        {
            bool isValid = char.IsAsciiLetterOrDigit(c) || c is '-' or '_';
            Assert.True(isValid, $"Character '{c}' is not a valid Base64Url character.");
        }
    }
}
