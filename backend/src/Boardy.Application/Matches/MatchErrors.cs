namespace Boardy.Application.Matches;

using Boardy.Application.Common.Results;

public static class MatchErrors
{
    public static readonly Error RoomNotFound = new(
        "Match.RoomNotFound",
        "The requested room was not found.");

    public static readonly Error UserNotFound = new(
        "Match.UserNotFound",
        "The specified host user does not exist.");

    public static readonly Error OnlyHostCanStartMatch = new(
        "Match.OnlyHostCanStartMatch",
        "Only the room host can start a match.");

    public static readonly Error RoomNotOpen = new(
        "Match.RoomNotOpen",
        "Cannot start a match when the room is not open.");

    public static readonly Error InvalidGameDefinitionId = new(
        "Match.InvalidGameDefinitionId",
        "The specified game definition identifier is invalid.");

    public static readonly Error ParticipantUserNotFound = new(
        "Match.ParticipantUserNotFound",
        "A participant user record could not be found.");

    public static readonly Error PersistenceFailed = new(
        "Match.PersistenceFailed",
        "Failed to persist match activation.");
}
