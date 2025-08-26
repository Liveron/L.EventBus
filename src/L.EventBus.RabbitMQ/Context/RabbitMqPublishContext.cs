using L.EventBus.Abstractions.Context;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqPublishContext(object payload) : IRabbitMqPublishContext
{
    public object Payload { get; set; } = payload;

    public IDictionary<string, object?> Headers { get; } = new Dictionary<string, object?>();
}
