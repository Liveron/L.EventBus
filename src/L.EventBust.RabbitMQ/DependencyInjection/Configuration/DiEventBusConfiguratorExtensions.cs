using L.EventBus.Abstractions;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBust.RabbitMQ.Configuration;
using L.EventBust.RabbitMQ.DependencyInjection.Configuration.Exchange;
using Microsoft.Extensions.DependencyInjection;

namespace L.EventBust.RabbitMQ.DependencyInjection.Configuration;

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
}