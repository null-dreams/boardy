namespace Boardy.Application.Rooms;

public sealed record CloseRoomCommand(Guid RoomId, Guid RequesterUserId);
