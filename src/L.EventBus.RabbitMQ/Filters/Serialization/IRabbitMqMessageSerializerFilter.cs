using L.EventBus.Abstractions.Filters;
using L.EventBus.RabbitMQ.Context;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Filters.Serialization;

public interface IRabbitMqMessageSerializerFilter : IMessageSerializerFilter<RabbitMqPublishContext>;
public interface IRabbitMqMessageSerializerFilter<TPayload>
    : IMessageSerializerFilter<RabbitMqPublishContext<TPayload>>, 
    IRabbitMqMessageSerializerFilter where TPayload : notnull
{
    Task IFilter<RabbitMqPublishContext>.HandleAsync(RabbitMqPublishContext context, FilterDelegate next) =>
        HandleAsync((RabbitMqPublishContext<TPayload>)context, next);
    Task IFilter.HandleAsync(IPipeContext context, FilterDelegate next) =>
        HandleAsync((RabbitMqPublishContext<TPayload>)context, next);
}
