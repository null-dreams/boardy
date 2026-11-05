namespace Boardy.Api.Tests;

public class ApiAssemblyTests
{
    [Fact]
    public void ApiAssembly_CanBeReferenced()
    {
        var assembly = typeof(ApiAssemblyTests).Assembly;
        Assert.NotNull(assembly);
    }
}

