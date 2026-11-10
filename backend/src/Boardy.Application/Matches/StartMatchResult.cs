namespace Boardy.Application.Matches;

using Boardy.Domain.Matches;

public sealed record StartMatchResult(
    Guid MatchId,
    Guid RoomId,
    string GameDefinitionId,
    MatchStatus Status,
    IReadOnlyList<MatchParticipantSnapshot> Participants);
