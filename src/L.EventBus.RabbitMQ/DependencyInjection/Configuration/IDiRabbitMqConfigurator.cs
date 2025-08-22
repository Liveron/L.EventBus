using L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration;

public interface IDiRabbitMqConfigurator
{
    void SetExchange(string type, string name, Action<IDiExchangeConfigurator>? config = null);
}