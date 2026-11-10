namespace Boardy.Application.Rooms;

using Boardy.Domain.Rooms;

public sealed record RoomDetails(
    Guid Id,
    RoomStatus Status,
    Guid HostUserId,
    RoomSettings Settings,
    string? Code,
    IReadOnlyList<RoomMemberDetails> Members);
