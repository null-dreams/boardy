namespace Boardy.Application.Identity;

using Boardy.Application.Common.Results;

public static class GuestIdentityErrors
{
    public static readonly Error InvalidDisplayName = new(
        "Guest.InvalidDisplayName",
        "The display name cannot be empty or contain only whitespace.");

    public static readonly Error InvalidRecoveryCredential = new(
        "Guest.InvalidRecoveryCredential",
        "The provided recovery credential is invalid.");

    public static readonly Error CredentialGenerationFailed = new(
        "Guest.CredentialGenerationFailed",
        "Failed to generate a secure recovery credential.");

    public static readonly Error PersistenceFailed = new(
        "Guest.PersistenceFailed",
        "Failed to persist the guest identity.");

    public static readonly Error InvalidEmail = new(
        "Guest.InvalidEmail",
        "The email address cannot be empty or contain only whitespace.");

    public static readonly Error InvalidPassword = new(
        "Guest.InvalidPassword",
        "The password cannot be empty or contain only whitespace.");

    public static readonly Error MissingGuestIdentifier = new(
        "Guest.MissingGuestIdentifier",
        "A valid guest user identifier or recovery credential must be provided.");

    public static readonly Error GuestUserNotFound = new(
        "Guest.UserNotFound",
        "The guest user was not found.");

    public static readonly Error UserAlreadyRegistered = new(
        "Guest.UserAlreadyRegistered",
        "The user is already a registered user and cannot be upgraded.");

    public static readonly Error GuestIdentityMismatch = new(
        "Guest.IdentityMismatch",
        "The provided recovery credential does not match the specified guest user.");

    public static readonly Error ProofOfOwnershipRequired = new(
        "Guest.ProofOfOwnershipRequired",
        "A caller-supplied guest user identifier is not sufficient proof of ownership. A recovery credential or authenticated identity is required.");

    public static Error CompensationFailed(Guid userId, string accountId, string rollbackDetails) =>
        new("Guest.CompensationFailed",
            $"Persistence failed for user '{userId}', and compensation rollback of account '{accountId}' also failed: {rollbackDetails}");
}
