namespace Boardy.Application.Identity;

using System.Security.Cryptography;
using System.Text;

public sealed class Sha256RecoveryCredentialHasher : IRecoveryCredentialHasher
{
    public string HashCredential(string credential)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(credential);

        byte[] inputBytes = Encoding.UTF8.GetBytes(credential);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToHexStringLower(hashBytes);
    }
}
