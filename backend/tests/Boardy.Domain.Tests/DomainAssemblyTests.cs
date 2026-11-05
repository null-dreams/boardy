namespace Boardy.Domain.Tests;

public class DomainAssemblyTests
{
    [Fact]
    public void DomainAssembly_CanBeReferenced()
    {
        var assembly = typeof(DomainAssemblyTests).Assembly;
        Assert.NotNull(assembly);
    }
}

