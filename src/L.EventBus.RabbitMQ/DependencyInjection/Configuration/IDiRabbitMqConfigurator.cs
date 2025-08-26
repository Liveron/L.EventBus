using L.EventBus.Abstractions;
using L.EventBus.Abstractions.Filters;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;
using L.EventBus.RabbitMQ.Filters;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration;

public interface IDiRabbitMqConfigurator
{
    void SetExchange(string type, string name, Action<IDiExchangeConfigurator>? config = null);
    void AddConsumeFilter<TFilter>() where TFilter : class, IRabbitMqConsumeFilter;
    void AddPublishFilter<TFilter>() where TFilter : class, IRabbitMqPublishFilter;
    void AddSubscription<TEvent, TEventHandler>(string queue) where TEventHandler : class, IEventHandler<TEvent>;
}