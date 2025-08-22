using L.EventBust.RabbitMQ.DependencyInjection.Configuration.Exchange;

namespace L.EventBust.RabbitMQ.DependencyInjection.Configuration;

public interface IDiRabbitMqConfigurator
{
    void SetExchange(string type, string name, Action<IDiExchangeConfigurator>? config = null);
}