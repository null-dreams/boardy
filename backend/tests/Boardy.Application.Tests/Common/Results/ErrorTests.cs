using Boardy.Application.Common.Results;

namespace Boardy.Application.Tests.Common.Results;

public class ErrorTests
{
    [Fact]
    public void Constructor_SetsCodeAndDescription()
    {
        var error = new Error("User.NotFound", "The user was not found.");

        Assert.Equal("User.NotFound", error.Code);
        Assert.Equal("The user was not found.", error.Description);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCodeIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Error(null!, "Description"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public void Constructor_ThrowsArgumentException_WhenCodeIsEmptyOrWhitespace(string invalidCode)
    {
        Assert.Throws<ArgumentException>(() => new Error(invalidCode, "Description"));
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenDescriptionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new Error("Code", null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\n")]
    public void Constructor_ThrowsArgumentException_WhenDescriptionIsEmptyOrWhitespace(string invalidDescription)
    {
        Assert.Throws<ArgumentException>(() => new Error("Code", invalidDescription));
    }

    [Fact]
    public void Equality_ReturnsTrue_ForIdenticalCodeAndDescription()
    {
        var error1 = new Error("Room.NotFound", "Room was not found.");
        var error2 = new Error("Room.NotFound", "Room was not found.");

        Assert.Equal(error1, error2);
        Assert.True(error1 == error2);
    }

    [Fact]
    public void Equality_ReturnsFalse_WhenCodeOrDescriptionDiffers()
    {
        var error1 = new Error("Room.NotFound", "Room was not found.");
        var error2 = new Error("Room.Invalid", "Room was not found.");
        var error3 = new Error("Room.NotFound", "Different message.");

        Assert.NotEqual(error1, error2);
        Assert.NotEqual(error1, error3);
        Assert.False(error1 == error2);
        Assert.False(error1 == error3);
    }
}
