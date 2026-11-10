namespace Boardy.Application.Identity;

using System.Buffers.Text;
using System.Security.Cryptography;

public sealed class CryptographicRecoveryCredentialGenerator : IRecoveryCredentialGenerator
{
    private const int ByteLength = 32;

    public string GenerateCredential()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(ByteLength);
        return Base64Url.EncodeToString(bytes);
    }
}
