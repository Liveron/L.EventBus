using L.EventBus.RabbitMQ.Filters.MessagePublishing;
using L.EventBus.RabbitMQ.Filters.Serialization;
using L.Pipes.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace L.EventBus.RabbitMQ.Filters.Resolver;

public class RabbitMqFiltersResolver
{
    public IEnumerable<IFilter> GetPublishFilters<TMessage>(IServiceProvider provider) where TMessage : notnull
    {
        var filters = provider.GetServices<IRabbitMqPublishFilter>();
        IRabbitMqMessageSerializerFilter? serializer;
        serializer = provider.GetService<IRabbitMqMessageSerializerFilter<TMessage>>();
        serializer ??= provider.GetRequiredService<IRabbitMqMessageSerializerFilter>();
        var publisher = provider.GetRequiredService<IRabbitMqMessagePublisherFilter>();
        return [.. filters, serializer, publisher];
    }
}
