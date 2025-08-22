using L.EventBus.Abstractions;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration;

public static class DiEventBusConfiguratorExtensions
{
    public static void SetExchange(this IDiEventBusConfigurator eventBusConfigurator, string type, string name,
        Action<IDiExchangeConfigurator>? config = null)
    {
        var configurator = new DiExchangeConfigurator(eventBusConfigurator.Services, name);
        config?.Invoke(configurator);

        var exchangeConfiguration = new ExchangeConfiguration(name, type);
        eventBusConfigurator.Services.Configure<RabbitMqEventBusConfiguration>(o =>
        {
            o.ExchangeConfigurations.Add(exchangeConfiguration);
        });
    }

    public static void UseRabbitMq(this IDiEventBusConfigurator eventBusConfigurator, string rabbitMqConnectionString,
        Action<IDiRabbitMqConfigurator>? config = null)
    {
        eventBusConfigurator.Services.AddOptions<RabbitMqEventBusConfiguration>();

        var configurator = new DiRabbitMqConfigurator(eventBusConfigurator.Services);
        config?.Invoke(configurator);

        var connection = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
        eventBusConfigurator.Services.AddSingleton(connection.CreateConnectionAsync().GetAwaiter().GetResult());

        eventBusConfigurator.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
        eventBusConfigurator.Services.AddHostedService(sp => (RabbitMqEventBus)sp.GetRequiredService<IEventBus>());
    }
}