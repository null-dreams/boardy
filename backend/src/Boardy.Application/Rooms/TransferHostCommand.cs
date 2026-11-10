namespace Boardy.Application.Rooms;

public sealed record TransferHostCommand(Guid RoomId, Guid RequesterUserId, Guid NewHostUserId);
