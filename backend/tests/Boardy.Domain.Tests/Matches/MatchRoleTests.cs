using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class MatchRoleTests
{
    [Fact]
    public void MatchRole_HasExpectedNumberOfMembers()
    {
        // Arrange & Act
        var names = Enum.GetNames<MatchRole>();

        // Assert
        Assert.Equal(2, names.Length);
    }

    [Theory]
    [InlineData(MatchRole.Participant, 0, "Participant")]
    [InlineData(MatchRole.Spectator, 1, "Spectator")]
    public void MatchRole_Values_HaveExpectedNamesAndUnderlyingValues(MatchRole role, int expectedValue, string expectedName)
    {
        // Assert
        Assert.Equal(expectedValue, (int)role);
        Assert.Equal(expectedName, role.ToString());
    }

    [Fact]
    public void MatchRole_AllExpectedValuesAreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(MatchRole.Participant));
        Assert.True(Enum.IsDefined(MatchRole.Spectator));
    }
}
