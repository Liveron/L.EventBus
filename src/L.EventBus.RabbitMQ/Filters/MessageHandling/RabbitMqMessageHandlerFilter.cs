using L.EventBus.Abstractions;
using L.EventBus.RabbitMQ.Context;
using L.Pipes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.Filters.MessageHandling;

public sealed class RabbitMqMessageHandlerFilter(IServiceProvider serviceProvider, IRabbitMqEventBus bus) 
    : IRabbitMqMessageHandlerFilter
{
    public async Task HandleAsync(RabbitMqConsumeContext context, FilterDelegate _)
    {
        var eventType = context.Payload.GetType();
        var handlers = serviceProvider.GetKeyedServices<IEventHandler>(eventType);

        foreach (var handler in handlers)
            await handler.HandleAsync(context.Payload);

        var channel = ((RabbitMqEventBus)bus).ConsumerChannel ?? 
            throw new InvalidOperationException("Канал потребления сообщений не инициализирован.");

        await channel.BasicAckAsync(context.DeliveryTag, multiple: false);
    }
}
