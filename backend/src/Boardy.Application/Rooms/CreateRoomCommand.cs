namespace Boardy.Application.Rooms;

using Boardy.Domain.Rooms;

public sealed record CreateRoomCommand(
    Guid HostUserId,
    RoomSettings? Settings = null);
