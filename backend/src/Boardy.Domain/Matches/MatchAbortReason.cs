namespace Boardy.Domain.Matches;

public enum MatchAbortReason
{
    HostAborted,
    AllPlayersDisconnected,
    InitializationFailed,
    AdministrativeTermination
}
