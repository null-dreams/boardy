namespace Boardy.Application.Rooms;

public sealed record RegenerateRoomCodeCommand(Guid RoomId, Guid RequesterUserId);
