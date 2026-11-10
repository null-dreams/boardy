using Boardy.Domain.Matches;

namespace Boardy.Domain.Tests.Matches;

public class ParticipationStatusTests
{
    [Fact]
    public void ParticipationStatus_HasExpectedNumberOfMembers()
    {
        // Arrange & Act
        var names = Enum.GetNames<ParticipationStatus>();

        // Assert
        Assert.Equal(3, names.Length);
    }

    [Theory]
    [InlineData(ParticipationStatus.Active, 0, "Active")]
    [InlineData(ParticipationStatus.Forfeited, 1, "Forfeited")]
    [InlineData(ParticipationStatus.Disconnected, 2, "Disconnected")]
    public void ParticipationStatus_Values_HaveExpectedNamesAndUnderlyingValues(ParticipationStatus status, int expectedValue, string expectedName)
    {
        // Assert
        Assert.Equal(expectedValue, (int)status);
        Assert.Equal(expectedName, status.ToString());
    }

    [Fact]
    public void ParticipationStatus_AllExpectedValuesAreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(ParticipationStatus.Active));
        Assert.True(Enum.IsDefined(ParticipationStatus.Forfeited));
        Assert.True(Enum.IsDefined(ParticipationStatus.Disconnected));
    }
}
