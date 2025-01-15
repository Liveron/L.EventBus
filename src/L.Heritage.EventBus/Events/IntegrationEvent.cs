namespace L.Heritage.EventBus.Events;

public record IntegrationEvent
{
    public IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedOn = DateTime.UtcNow;
    }

    public Guid Id { get; init; }

    public DateTime CreatedOn { get; init; }
}
