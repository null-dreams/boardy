namespace Boardy.Application.Rooms;

public sealed record JoinRoomCommand(
    Guid UserId,
    string RoomCode);
