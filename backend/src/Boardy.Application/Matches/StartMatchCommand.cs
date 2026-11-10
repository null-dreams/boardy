namespace Boardy.Application.Matches;

public sealed record StartMatchCommand(
    Guid RoomId,
    Guid HostUserId,
    string GameDefinitionId);
