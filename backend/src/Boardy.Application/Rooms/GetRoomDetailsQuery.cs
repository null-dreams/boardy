namespace Boardy.Application.Rooms;

public sealed record GetRoomDetailsQuery(Guid RoomId, Guid RequesterUserId);
