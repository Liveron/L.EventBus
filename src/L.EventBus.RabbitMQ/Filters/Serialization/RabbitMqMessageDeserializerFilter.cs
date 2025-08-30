using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace L.EventBus.RabbitMQ.Filters.Serialization;

public class RabbitMqMessageDeserializerFilter(
    IOptions<EventBusInfo> info, ILogger<RabbitMqMessageDeserializerFilter>? logger = null) 
    : IRabbitMqMessageDeserializerFilter
{
    private readonly EventBusInfo eventBusInfo = info.Value;

    public async Task HandleAsync(RabbitMqConsumeContext<ReadOnlyMemory<byte>> context, FilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(context.EventName))
        {
            logger?.LogWarning("Message does not contain event name header. Message will be ignored.");
            return;
        }

        var messageString = Encoding.UTF8.GetString(context.Payload.Span);
        if (!eventBusInfo.EventTypes.TryGetValue(context.EventName, out var eventType))
        {
            logger?.LogWarning("Unable to resolve event type for event name {EventName}", eventType);
            return;
        }

        var message = JsonSerializer.Deserialize(messageString, eventType);
        if (message is null)
        {
            logger?.LogWarning(
                "Message deserialization returned null for event name {EventName}", context.EventName);
            return;
        }

        var consumeContext = new RabbitMqConsumeContext(message, context.DeliveryTag)
        {
            Headers = context.Headers,
            EventName = context.EventName,
        };
        await next(consumeContext);
    }
}
