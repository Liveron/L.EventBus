using L.EventBus.Abstractions.Context;

namespace L.EventBus.RabbitMQ.Context;

public interface IRabbitMqPublishContext : IPublishContext
{
    public IDictionary<string, object?> Headers { get; }
}
