using L.EventBus.RabbitMQ.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;

public sealed class DiExchangeConfigurator(IServiceCollection services, string exchangeName) : IDiExchangeConfigurator
{
    public void SetQueue(string name, string routingKey)
    {
        var configuration = new QueueConfiguration(name, exchangeName, routingKey);
        services.Configure<RabbitMqEventBusConfiguration>(o =>
        {
            if (o.QueueConfigurations.All(q => q.Name != name))
            {
                o.QueueConfigurations.Add(configuration);
            }
        });
    }

    public void SetMessage<TMessage>(string routingKey)
    {
        var configuration = new MessageConfiguration(exchangeName, routingKey);
        services.Configure<RabbitMqEventBusConfiguration>(o =>
        {
            o.MessageConfigurations[typeof(TMessage)] = configuration;
        });
    }
}