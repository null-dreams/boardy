using System.Reflection;
using Boardy.Domain.Rooms;

namespace Boardy.Domain.Tests.Rooms;

public class RoomSettingsTests
{
    [Fact]
    public void Default_HasExpectedConfigurationValues()
    {
        // Act
        var settings = RoomSettings.Default;

        // Assert
        Assert.Equal(4, settings.MaxMembers);
        Assert.False(settings.AllowSpectators);
    }

    [Fact]
    public void Create_WithCustomConfiguration_SetsPropertiesCorrectly()
    {
        // Act
        var settings = RoomSettings.Create(6, true);

        // Assert
        Assert.Equal(6, settings.MaxMembers);
        Assert.True(settings.AllowSpectators);
    }

    [Fact]
    public void Create_WithMinimumCapacity_Succeeds()
    {
        // Act
        var settings = RoomSettings.Create(2);

        // Assert
        Assert.Equal(2, settings.MaxMembers);
        Assert.False(settings.AllowSpectators);
    }

    [Fact]
    public void Create_WithMaximumCapacity_Succeeds()
    {
        // Act
        var settings = RoomSettings.Create(16);

        // Assert
        Assert.Equal(16, settings.MaxMembers);
        Assert.False(settings.AllowSpectators);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithCapacityBelowMinimum_ThrowsArgumentOutOfRangeException(int invalidCapacity)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RoomSettings.Create(invalidCapacity));
    }

    [Theory]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(100)]
    public void Create_WithCapacityAboveMaximum_ThrowsArgumentOutOfRangeException(int invalidCapacity)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => RoomSettings.Create(invalidCapacity));
    }

    [Fact]
    public void Create_WithoutSpecifyingAllowSpectators_DefaultsToFalse()
    {
        // Act
        var settings = RoomSettings.Create(4);

        // Assert
        Assert.False(settings.AllowSpectators);
    }

    [Fact]
    public void Create_WithAllowSpectatorsTrue_EnablesSpectators()
    {
        // Act
        var settings = RoomSettings.Create(4, allowSpectators: true);

        // Assert
        Assert.True(settings.AllowSpectators);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnTrueForIdenticalValues()
    {
        // Arrange
        var settings1 = RoomSettings.Create(4, true);
        var settings2 = RoomSettings.Create(4, true);

        // Assert
        Assert.True(settings1.Equals(settings2));
        Assert.True(settings1.Equals((object)settings2));
        Assert.True(settings1 == settings2);
        Assert.False(settings1 != settings2);
    }

    [Fact]
    public void Equals_And_EqualityOperators_ReturnFalseWhenEitherPropertyDiffers()
    {
        // Arrange
        var baseSettings = RoomSettings.Create(4, false);
        var differentCapacity = RoomSettings.Create(6, false);
        var differentSpectators = RoomSettings.Create(4, true);

        // Assert
        Assert.False(baseSettings.Equals(differentCapacity));
        Assert.False(baseSettings == differentCapacity);
        Assert.True(baseSettings != differentCapacity);

        Assert.False(baseSettings.Equals(differentSpectators));
        Assert.False(baseSettings == differentSpectators);
        Assert.True(baseSettings != differentSpectators);

        Assert.False(baseSettings.Equals(null));
        Assert.False(baseSettings.Equals((object?)null));
        Assert.False(baseSettings.Equals("SomeString"));
        Assert.False(baseSettings == null);
        Assert.False(null == baseSettings);
        Assert.True(baseSettings != null);
        Assert.True(null != baseSettings);
    }

    [Fact]
    public void GetHashCode_ReturnsSameHashCodeForEqualInstances()
    {
        // Arrange
        var settings1 = RoomSettings.Create(8, true);
        var settings2 = RoomSettings.Create(8, true);

        // Assert
        Assert.Equal(settings1.GetHashCode(), settings2.GetHashCode());
    }

    [Fact]
    public void PublicProperties_AreImmutable()
    {
        // Arrange
        var maxMembersProperty = typeof(RoomSettings).GetProperty(nameof(RoomSettings.MaxMembers));
        var allowSpectatorsProperty = typeof(RoomSettings).GetProperty(nameof(RoomSettings.AllowSpectators));

        // Assert
        Assert.NotNull(maxMembersProperty);
        Assert.True(maxMembersProperty.CanRead);
        Assert.False(maxMembersProperty.CanWrite, "MaxMembers should not have a setter.");

        Assert.NotNull(allowSpectatorsProperty);
        Assert.True(allowSpectatorsProperty.CanRead);
        Assert.False(allowSpectatorsProperty.CanWrite, "AllowSpectators should not have a setter.");
    }

    [Fact]
    public void ToString_ReturnsInformativeString()
    {
        // Arrange
        var settings = RoomSettings.Create(4, true);

        // Act & Assert
        Assert.Equal("MaxMembers: 4, AllowSpectators: True", settings.ToString());
    }
}

