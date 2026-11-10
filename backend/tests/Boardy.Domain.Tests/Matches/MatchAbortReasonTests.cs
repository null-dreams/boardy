using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchAbortReasonTests
{
    [Fact]
    public void MatchAbortReason_HasExpectedNumberOfMembers()
    {
        // Arrange & Act
        var names = Enum.GetNames<MatchAbortReason>();

        // Assert
        Assert.Equal(4, names.Length);
    }

    [Theory]
    [InlineData(MatchAbortReason.HostAborted, 0, "HostAborted")]
    [InlineData(MatchAbortReason.AllPlayersDisconnected, 1, "AllPlayersDisconnected")]
    [InlineData(MatchAbortReason.InitializationFailed, 2, "InitializationFailed")]
    [InlineData(MatchAbortReason.AdministrativeTermination, 3, "AdministrativeTermination")]
    public void MatchAbortReason_Values_HaveExpectedNamesAndUnderlyingValues(MatchAbortReason reason, int expectedValue, string expectedName)
    {
        // Assert
        Assert.Equal(expectedValue, (int)reason);
        Assert.Equal(expectedName, reason.ToString());
    }

    [Fact]
    public void MatchAbortReason_AllExpectedValuesAreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(MatchAbortReason.HostAborted));
        Assert.True(Enum.IsDefined(MatchAbortReason.AllPlayersDisconnected));
        Assert.True(Enum.IsDefined(MatchAbortReason.InitializationFailed));
        Assert.True(Enum.IsDefined(MatchAbortReason.AdministrativeTermination));
    }
}
