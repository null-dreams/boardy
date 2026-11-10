namespace Boardy.Application.Identity;

using Boardy.Domain.Users;

public sealed record CreateGuestResult(User User, string RecoveryCredential);
