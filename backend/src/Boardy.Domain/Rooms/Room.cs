namespace Boardy.Domain.Rooms;

public sealed class Room
{
    private readonly List<RoomMembership> _memberships = [];

    public Guid Id { get; }
    public RoomCode Code { get; }
    public Guid HostUserId { get; private set; }
    public RoomStatus Status { get; private set; }
    public RoomSettings Settings { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<RoomMembership> Memberships => _memberships.AsReadOnly();

    private Room(
        Guid id,
        RoomCode code,
        Guid hostUserId,
        RoomStatus status,
        RoomSettings settings,
        DateTimeOffset createdAt,
        RoomMembership hostMembership)
    {
        Id = id;
        Code = code;
        HostUserId = hostUserId;
        Status = status;
        Settings = settings;
        CreatedAt = createdAt;
        _memberships.Add(hostMembership);
    }

    public static Room Create(Guid hostUserId, RoomCode code, RoomSettings settings)
    {
        if (hostUserId == Guid.Empty)
        {
            throw new ArgumentException("Host user ID cannot be empty.", nameof(hostUserId));
        }

        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(settings);

        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var hostMembership = RoomMembership.Create(hostUserId, RoomRole.Host);

        return new Room(id, code, hostUserId, RoomStatus.Open, settings, createdAt, hostMembership);
    }
}
