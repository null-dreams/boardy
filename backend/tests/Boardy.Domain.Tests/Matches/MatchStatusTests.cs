using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchStatusTests
{
    [Fact]
    public void MatchStatus_HasExpectedNumberOfMembers()
    {
        // Arrange & Act
        var names = Enum.GetNames<MatchStatus>();

        // Assert
        Assert.Equal(5, names.Length);
    }

    [Theory]
    [InlineData(MatchStatus.Pending, 0, "Pending")]
    [InlineData(MatchStatus.Initializing, 1, "Initializing")]
    [InlineData(MatchStatus.Active, 2, "Active")]
    [InlineData(MatchStatus.Completed, 3, "Completed")]
    [InlineData(MatchStatus.Aborted, 4, "Aborted")]
    public void MatchStatus_Values_HaveExpectedNamesAndUnderlyingValues(MatchStatus status, int expectedValue, string expectedName)
    {
        // Assert
        Assert.Equal(expectedValue, (int)status);
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void MatchStatus_AllExpectedValuesAreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(MatchStatus.Pending));
        Assert.True(Enum.IsDefined(MatchStatus.Initializing));
        Assert.True(Enum.IsDefined(MatchStatus.Active));
        Assert.True(Enum.IsDefined(MatchStatus.Completed));
        Assert.True(Enum.IsDefined(MatchStatus.Aborted));
    }
}
