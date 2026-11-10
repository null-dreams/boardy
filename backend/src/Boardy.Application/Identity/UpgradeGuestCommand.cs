namespace Boardy.Application.Identity;

public sealed record UpgradeGuestCommand(
    string Email,
    string Password,
    Guid? GuestUserId = null,
    string? RecoveryCredential = null,
    bool IsAuthenticatedIdentity = false)
{
    public static UpgradeGuestCommand FromRecoveryCredential(
        string email,
        string password,
        string recoveryCredential,
        Guid? expectedGuestUserId = null) =>
        new(email, password, expectedGuestUserId, recoveryCredential, IsAuthenticatedIdentity: false);

    public static UpgradeGuestCommand FromAuthenticatedIdentity(
        string email,
        string password,
        Guid authenticatedUserId) =>
        new(email, password, authenticatedUserId, null, IsAuthenticatedIdentity: true);
}
