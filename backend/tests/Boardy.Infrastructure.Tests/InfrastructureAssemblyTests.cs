namespace Boardy.Infrastructure.Tests;

public class InfrastructureAssemblyTests
{
    [Fact]
    public void InfrastructureAssembly_CanBeReferenced()
    {
        var assembly = typeof(InfrastructureAssemblyTests).Assembly;
        Assert.NotNull(assembly);
    }
}

