using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqPublishContext(object payload, string routingKey, string exchange, string eventName) : IPipeContext
{
    public object Payload { get; set; } = payload;
    public IDictionary<string, object?> Headers { get; } = new Dictionary<string, object?>();
    public string RoutingKey { get; set; } = routingKey;
    public string Exchange { get; set; } = exchange;
    public string EventName { get; set; } = eventName;
}
