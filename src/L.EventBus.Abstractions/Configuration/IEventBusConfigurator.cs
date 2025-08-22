namespace L.EventBus.Abstractions.Configuration;

public interface IEventBusConfigurator
{
    public IEventBusConfigurator AddSubscription<TEvent, TEventHandler>()
        where TEventHandler : class, IEventHandler<TEvent>;
}