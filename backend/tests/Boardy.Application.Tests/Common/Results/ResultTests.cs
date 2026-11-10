using System.Reflection;
using Boardy.Application.Common.Results;

namespace Boardy.Application.Tests.Common.Results;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Success_ThrowsInvalidOperationException_WhenAccessingError()
    {
        var result = Result.Success();

        var exception = Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        Assert.Equal("Cannot access Error on a successful result.", exception.Message);
    }

    [Fact]
    public void Failure_CreatesFailedResult_WithPreservedError()
    {
        var error = new Error("Match.NotFound", "Match was not found.");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_ThrowsArgumentNullException_WhenErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }

    [Fact]
    public void Constructors_AreInaccessiblePublicly()
    {
        var publicConstructors = typeof(Result).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(publicConstructors);
    }
}
