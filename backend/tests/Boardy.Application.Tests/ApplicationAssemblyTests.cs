namespace Boardy.Application.Tests;

public class ApplicationAssemblyTests
{
    [Fact]
    public void ApplicationAssembly_CanBeReferenced()
    {
        var assembly = typeof(ApplicationAssemblyTests).Assembly;
        Assert.NotNull(assembly);
    }
}

