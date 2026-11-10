namespace Boardy.Application.Rooms;

using Boardy.Domain.Rooms;

public sealed record RoomMemberDetails(
    Guid UserId,
    RoomRole Role,
    DateTimeOffset JoinedAt,
    string? DisplayName = null);
