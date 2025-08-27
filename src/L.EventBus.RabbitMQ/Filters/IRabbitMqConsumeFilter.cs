using L.EventBus.RabbitMQ.Context;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Filters;

public interface IRabbitMqConsumeFilter : IFilter<RabbitMqConsumeContext>;

public interface IRabbitMqConsumeFilter<TPayload>
    : IFilter<RabbitMqConsumeContext<TPayload>>, IRabbitMqConsumeFilter where TPayload : notnull
{
    Task IFilter.HandleAsync(IPipeContext context, FilterDelegate next)
        => HandleAsync((RabbitMqConsumeContext<TPayload>)context, next);

    Task IFilter<RabbitMqConsumeContext>.HandleAsync(RabbitMqConsumeContext context, FilterDelegate next)
        => HandleAsync((RabbitMqConsumeContext<TPayload>)context, next);
}