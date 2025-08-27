using L.EventBus.Abstractions;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.Filters.MessageHandling;
using L.EventBus.RabbitMQ.Filters.MessagePublishing;
using L.EventBus.RabbitMQ.Filters.Serialization;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration;

public static class DiEventBusConfiguratorExtensions
{
    public static void UseRabbitMq(this IDiEventBusConfigurator eventBusConfigurator, string rabbitMqConnectionString,
        Action<IDiRabbitMqConfigurator>? config = null)
    {
        eventBusConfigurator.Services.AddOptions<RabbitMqEventBusConfiguration>();

        eventBusConfigurator.Services.AddDefaultFilters();

        var configurator = new DiRabbitMqConfigurator(eventBusConfigurator.Services);
        config?.Invoke(configurator);

        var connectionFactory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
        var connection = connectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
        eventBusConfigurator.Services.AddSingleton(connection);

        eventBusConfigurator.Services.AddSingleton<IRabbitMqEventBus, RabbitMqEventBus>();
        eventBusConfigurator.Services.AddHostedService(sp => (RabbitMqEventBus)sp.GetRequiredService<IEventBus>());
    }

    private static void AddDefaultFilters(this IServiceCollection services)
    {
        services.AddTransient<IRabbitMqMessageSerializerFilter, RabbitMqMessageSerializerFilter>();
        services.AddTransient<IRabbitMqMessagePublisherFilter, RabbitMqMessagePublisherFilter>();

        services.AddTransient<IRabbitMqMessageDeserializerFilter, RabbitMqMessageDeserializerFilter>();
        services.AddTransient<IRabbitMqMessageHandlerFilter, RabbitMqMessageHandlerFilter>();
    }
}