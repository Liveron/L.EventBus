namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;

public interface IDiExchangeConfigurator
{
    void SetQueue(string name, string routingKey);
    void SetMessage<TMessage>(string routingKey);
}