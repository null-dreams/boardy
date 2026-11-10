namespace Boardy.Application.Rooms;

using Boardy.Application.Common.Results;

public static class RoomErrors
{
    public static readonly Error HostUserNotFound = new(
        "Room.HostUserNotFound",
        "The specified host user does not exist.");

    public static readonly Error UserNotFound = new(
        "Room.UserNotFound",
        "The specified user does not exist.");

    public static readonly Error InvalidRoomCode = new(
        "Room.InvalidRoomCode",
        "The specified room code is invalid.");

    public static readonly Error RoomNotFound = new(
        "Room.NotFound",
        "The requested room was not found.");

    public static readonly Error RoomCodeGenerationExhausted = new(
        "Room.CodeGenerationExhausted",
        "Failed to generate a unique room code after maximum retry attempts.");

    public static readonly Error RoomNotOpen = new(
        "Room.NotOpen",
        "Cannot join a room that is not open.");

    public static readonly Error RoomFull = new(
        "Room.Full",
        "The room has reached its maximum capacity.");

    public static readonly Error UserAlreadyMember = new(
        "Room.UserAlreadyMember",
        "The user is already a member of this room.");

    public static readonly Error PersistenceFailed = new(
        "Room.PersistenceFailed",
        "Failed to persist room changes.");

    public static readonly Error CannotJoin = new(
        "Room.CannotJoin",
        "Unable to join the room.");
}
