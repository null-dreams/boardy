using System.Reflection;
using Boardy.Domain.Events;

namespace Boardy.Domain.Tests.Events;

public class DomainEventTests
{
    private sealed record TestDomainEvent(string Data) : DomainEvent;

    private sealed record CustomConstructedDomainEvent : DomainEvent
    {
        public CustomConstructedDomainEvent(Guid eventId, DateTimeOffset occurredAt)
            : base(eventId, occurredAt)
        {
        }
    }

    [Fact]
    public void EventId_IsGeneratedAndNonEmpty()
    {
        // Act
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
    }

    [Fact]
    public void EventId_IsUniqueAcrossInstances()
    {
        // Arrange
        const int count = 1000;
        var eventIds = new HashSet<Guid>();

        // Act & Assert
        for (var i = 0; i < count; i++)
        {
            var domainEvent = new TestDomainEvent($"test-{i}");
            Assert.True(eventIds.Add(domainEvent.EventId), $"Duplicate EventId '{domainEvent.EventId}' detected.");
        }
    }

    [Fact]
    public void OccurredAt_IsInitializedInUtc()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var domainEvent = new TestDomainEvent("test");
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredAt.Offset);
        Assert.InRange(domainEvent.OccurredAt, before, after);
    }

    [Fact]
    public void Implements_IDomainEvent_Interface()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("test");

        // Assert
        Assert.IsAssignableFrom<IDomainEvent>(domainEvent);
        IDomainEvent iface = domainEvent;
        Assert.Equal(domainEvent.EventId, iface.EventId);
        Assert.Equal(domainEvent.OccurredAt, iface.OccurredAt);
    }

    [Fact]
    public void MetadataProperties_DoNotExposeSetters()
    {
        // Arrange
        var properties = typeof(DomainEvent).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.Contains(properties, p => p.Name == nameof(DomainEvent.EventId));
        Assert.Contains(properties, p => p.Name == nameof(DomainEvent.OccurredAt));

        foreach (var property in properties)
        {
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void IDomainEvent_Properties_DoNotExposeSetters()
    {
        // Arrange
        var properties = typeof(IDomainEvent).GetProperties();

        // Assert
        Assert.Contains(properties, p => p.Name == nameof(IDomainEvent.EventId));
        Assert.Contains(properties, p => p.Name == nameof(IDomainEvent.OccurredAt));

        foreach (var property in properties)
        {
            Assert.Null(property.SetMethod);
        }
    }

    [Fact]
    public void DerivedEvent_CanBeConstructedWithoutDuplicatingMetadataLogic()
    {
        // Act
        var derived = new TestDomainEvent("payload-123");

        // Assert
        Assert.Equal("payload-123", derived.Data);
        Assert.NotEqual(Guid.Empty, derived.EventId);
        Assert.Equal(TimeSpan.Zero, derived.OccurredAt.Offset);
    }

    [Fact]
    public void CustomConstructor_WithValidParameters_SetsPropertiesAndNormalizesToUtc()
    {
        // Arrange
        var customId = Guid.NewGuid();
        var localTime = new DateTimeOffset(2026, 11, 10, 10, 30, 0, TimeSpan.FromHours(5));

        // Act
        var domainEvent = new CustomConstructedDomainEvent(customId, localTime);

        // Assert
        Assert.Equal(customId, domainEvent.EventId);
        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredAt.Offset);
        Assert.Equal(localTime.ToUniversalTime(), domainEvent.OccurredAt);
    }

    [Fact]
    public void CustomConstructor_WithEmptyEventId_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new CustomConstructedDomainEvent(Guid.Empty, DateTimeOffset.UtcNow));
        Assert.Equal("eventId", ex.ParamName);
    }
}
