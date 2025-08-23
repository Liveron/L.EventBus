using L.EventBus.Abstractions.Filters;

namespace L.EventBus.Abstractions.Configuration;

public interface IEventBusConfigurator
{
    public IEventBusConfigurator AddSubscription<TEvent, TEventHandler>()
        where TEventHandler : class, IEventHandler<TEvent>;

    public void SetMessageEnvelope(Type envelopeType);

    public void AddConsumeFilter<TFilter>() where TFilter : class, IConsumeFilter;
}