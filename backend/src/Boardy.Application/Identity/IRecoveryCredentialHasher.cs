namespace Boardy.Application.Identity;

public interface IRecoveryCredentialHasher
{
    string HashCredential(string credential);
}
