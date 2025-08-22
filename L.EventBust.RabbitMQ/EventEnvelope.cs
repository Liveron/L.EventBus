namespace L.EventBust.RabbitMQ;

public sealed record EventEnvelope<TEvent>
{
    public TEvent Payload { get; private init; }
    public EventEnvelopeMetadata Meta { get; private init; }

    public EventEnvelope(TEvent @event, string version, string source,
        Guid correlationId, Guid causationId = default)
    {
        Payload = @event;
        Meta = new EventEnvelopeMetadata
        {
            Version = version,
            Source = source,
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }
}

public sealed record EventEnvelopeMetadata
{
    public required string Version { get; init; }
    public required string Source { get; init; }
    public required Guid CorrelationId { get; init; }
    public Guid CausationId { get; init; } = Guid.Empty;
}