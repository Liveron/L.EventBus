using L.EventBus.RabbitMQ.Context;
using L.Pipes.Abstractions;
using System.Text.Json;

namespace L.EventBus.RabbitMQ.Filters.Serialization;

public class RabbitMqMessageSerializerFilter : IRabbitMqMessageSerializerFilter
{
    public async Task HandleAsync(RabbitMqPublishContext context, FilterDelegate next)
    {
        context.Payload = JsonSerializer.SerializeToUtf8Bytes(context.Payload, context.Payload.GetType());
        await next(context);
    }
}
