namespace L.EventBus.Abstractions.Configuration;

public interface IMessageEnvelope<TMessage>
{
    TMessage Payload { get; init; }
}
