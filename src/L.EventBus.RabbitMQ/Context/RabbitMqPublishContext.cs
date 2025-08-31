using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqPublishContext(
    object payload, string routingKey, string exchange, string eventName)
    : IPipeContext
{
    public object Payload { get; set; } = payload;
    public IDictionary<string, object?> Headers { get; set; } = new Dictionary<string, object?>();
    public string RoutingKey { get; set; } = routingKey;
    public string Exchange { get; set; } = exchange;
    public string EventName { get; set; } = eventName;
}

public class RabbitMqPublishContext<TPayload>(
    TPayload payload, string routingKey, string exchange, string eventName)
    : RabbitMqPublishContext(payload, routingKey, exchange, eventName), 
    IPipeContext<TPayload> where TPayload : notnull
{
    public new TPayload Payload
    {
        get => (TPayload)base.Payload;
        set => base.Payload = value;
    }
}