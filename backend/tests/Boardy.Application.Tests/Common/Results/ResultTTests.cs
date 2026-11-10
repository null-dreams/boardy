using System.Reflection;
using Boardy.Application.Common.Results;

namespace Boardy.Application.Tests.Common.Results;

public class ResultTTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult_WithValue()
    {
        var result = Result<string>.Success("test-value");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public void Success_ViaStaticFactoryOnResult_CreatesSuccessfulResult()
    {
        Result<int> result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Success_ThrowsArgumentNullException_WhenValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Success<string>(null!));
    }

    [Fact]
    public void Success_ThrowsInvalidOperationException_WhenAccessingError()
    {
        var result = Result<int>.Success(100);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = result.Error);
        Assert.Equal("Cannot access Error on a successful result.", exception.Message);
    }

    [Fact]
    public void Failure_CreatesFailedResult_WithPreservedError()
    {
        var error = new Error("User.InvalidCredentials", "The credentials provided are invalid.");

        var result = Result<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_ViaStaticFactoryOnResult_CreatesFailedResult()
    {
        var error = new Error("Room.Full", "The room has reached maximum capacity.");

        Result<Guid> result = Result.Failure<Guid>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Failure_ThrowsArgumentNullException_WhenErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<string>(null!));
    }

    [Fact]
    public void Failure_ThrowsInvalidOperationException_WhenAccessingValue()
    {
        var error = new Error("Match.Terminated", "Match has already completed.");
        var result = Result<string>.Failure(error);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        Assert.Equal("Cannot access Value on a failed result.", exception.Message);
    }

    [Fact]
    public void Constructors_AreInaccessiblePublicly()
    {
        var publicConstructors = typeof(Result<string>).GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.Empty(publicConstructors);
    }

    [Fact]
    public void GenericResult_IsInstanceOfNonGenericResult()
    {
        var success = Result<int>.Success(10);
        var failure = Result<int>.Failure(new Error("Code", "Description"));

        Assert.IsAssignableFrom<Result>(success);
        Assert.IsAssignableFrom<Result>(failure);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-100)]
    public void SupportsValueTypes_WithDefaultAndNonDefaultValues(int value)
    {
        var result = Result<int>.Success(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void SupportsBooleanValueType()
    {
        var trueResult = Result<bool>.Success(true);
        var falseResult = Result<bool>.Success(false);

        Assert.True(trueResult.IsSuccess);
        Assert.True(trueResult.Value);

        Assert.True(falseResult.IsSuccess);
        Assert.False(falseResult.Value);
    }
}
