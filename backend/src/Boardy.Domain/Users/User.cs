namespace Boardy.Domain.Users;

public sealed class User
{
    public Guid Id { get; }
    public string DisplayName { get; private set; }
    public UserKind Kind { get; private set; }
    public DateTimeOffset CreatedAt { get; }

    private User(Guid id, string displayName, UserKind kind, DateTimeOffset createdAt)
    {
        Id = id;
        DisplayName = displayName;
        Kind = kind;
        CreatedAt = createdAt;
    }

    public static User CreateGuest(string displayName)
    {
        return Create(displayName, UserKind.Guest);
    }

    public static User CreateRegistered(string displayName)
    {
        return Create(displayName, UserKind.Registered);
    }

    public void UpgradeToRegistered()
    {
        if (Kind != UserKind.Guest)
        {
            throw new InvalidOperationException("Only guest users can be upgraded to registered.");
        }

        Kind = UserKind.Registered;
    }

    private static User Create(string displayName, UserKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new User(
            Guid.NewGuid(),
            displayName.Trim(),
            kind,
            DateTimeOffset.UtcNow);
    }
}
