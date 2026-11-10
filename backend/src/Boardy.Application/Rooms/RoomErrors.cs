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

    public static readonly Error UserNotMember = new(
        "Room.UserNotMember",
        "The specified user is not a member of this room.");

    public static readonly Error OnlyHostCanTransferHost = new(
        "Room.OnlyHostCanTransferHost",
        "Only the room host can transfer the host role.");

    public static readonly Error CannotTransferHostToSelf = new(
        "Room.CannotTransferHostToSelf",
        "Cannot transfer host role to the current host.");

    public static readonly Error ProposedHostNotMember = new(
        "Room.ProposedHostNotMember",
        "The proposed host is not a member of this room.");

    public static readonly Error HostCannotLeaveWithoutTransfer = new(
        "Room.HostCannotLeaveWithoutTransfer",
        "The room host cannot leave without transferring the host role or closing the room.");

    public static readonly Error RoomClosed = new(
        "Room.Closed",
        "Cannot perform this operation on a closed room.");

    public static readonly Error CannotCloseRoom = new(
        "Room.CannotCloseRoom",
        "Cannot close a room that is not open.");

    public static readonly Error CannotLeaveDuringMatch = new(
        "Room.CannotLeaveDuringMatch",
        "Cannot leave a room while a match is starting or in progress.");
}
