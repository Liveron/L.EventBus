using L.EventBus.Abstractions.Context;
using L.EventBus.Abstractions.Filters;
using L.EventBus.RabbitMQ.Context;

namespace L.EventBus.RabbitMQ.Filters;

public interface IRabbitMqPublishFilter : IPublishFilter
{
    Task PublishAsync(IRabbitMqPublishContext context, RabbitMqPublishDelegate next);
    Task IPublishFilter.PublishAsync(IPublishContext context, PublishDelegate next)
        => PublishAsync((IRabbitMqPublishContext)context, ctx => next(ctx));
}
