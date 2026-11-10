namespace Boardy.Domain.Events;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }

    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(Guid eventId, DateTimeOffset occurredAt)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event ID cannot be empty.", nameof(eventId));
        }

        EventId = eventId;
        OccurredAt = occurredAt.ToUniversalTime();
    }
}
