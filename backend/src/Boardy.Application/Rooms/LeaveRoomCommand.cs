namespace Boardy.Application.Rooms;

public sealed record LeaveRoomCommand(Guid RoomId, Guid UserId, Guid? NewHostUserId = null);
