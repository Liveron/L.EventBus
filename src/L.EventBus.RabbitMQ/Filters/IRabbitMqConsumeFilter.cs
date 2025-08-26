using L.EventBus.Abstractions.Filters;

namespace L.EventBus.RabbitMQ.Filters;

public interface IRabbitMqConsumeFilter : IConsumeFilter
{
    public Task ConsumeAsync();
}
