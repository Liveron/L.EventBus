using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.Filters.MessagePublishing;

public class RabbitMqMessagePublisherFilter(IConnection connection) 
    : IRabbitMqMessagePublisherFilter
{
    public async Task HandleAsync(RabbitMqPublishContext context, FilterDelegate next)
    {
        var properties = CreateBasicProperties(context.Headers, context.EventName);

        await using var channel = await connection.CreateChannelAsync();

        await channel.BasicPublishAsync(
            exchange: context.Exchange,
            routingKey: context.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: (byte[])context.Payload);
    }

    private static BasicProperties CreateBasicProperties(IDictionary<string, object?> headers, string eventName)
    {
        var properties = new BasicProperties
        {
            Headers = headers
        };
        properties.Headers.SetEventName(eventName);
        return properties;
    }
}
