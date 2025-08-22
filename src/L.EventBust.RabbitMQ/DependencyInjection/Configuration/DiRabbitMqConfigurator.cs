using L.EventBus.DependencyInjection.Configuration;
using L.EventBust.RabbitMQ.Configuration;
using L.EventBust.RabbitMQ.DependencyInjection.Configuration.Exchange;
using Microsoft.Extensions.DependencyInjection;

namespace L.EventBust.RabbitMQ.DependencyInjection.Configuration;

public sealed class DiRabbitMqConfigurator(IServiceCollection services) : IDiRabbitMqConfigurator
{
    public void SetExchange(string type, string name, Action<IDiExchangeConfigurator>? config = null)
    {
        var configurator = new DiExchangeConfigurator(services, name);
        config?.Invoke(configurator);

        var exchangeConfiguration = new ExchangeConfiguration(name, type);
        services.Configure<RabbitMqEventBusConfiguration>(o =>
        {
            o.ExchangeConfigurations.Add(exchangeConfiguration);
        });
    }
}