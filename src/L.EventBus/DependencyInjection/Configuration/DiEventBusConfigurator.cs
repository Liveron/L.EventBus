using L.EventBus.Abstractions;
using L.EventBus.Abstractions.Configuration;
using L.EventBus.Abstractions.Filters;

namespace L.EventBus.DependencyInjection.Configuration;

public sealed class DiEventBusConfigurator(
    IServiceCollection services) : IDiEventBusConfigurator
{
    public IServiceCollection Services => services;

    public void AddConsumeFilter<TFilter>() where TFilter : class, IConsumeFilter
    {
        Services.AddTransient<IConsumeFilter, TFilter>();
    }

    public IEventBusConfigurator AddSubscription<TEvent, TEventHandler>()
        where TEventHandler : class, IEventHandler<TEvent>
    {
        Services.AddKeyedTransient<IEventHandler, TEventHandler>(typeof(TEvent));

        Services.Configure<EventBusInfo>(o =>
        {
            o.EventTypes[typeof(TEvent).Name] = typeof(TEvent);
        });

        return this;
    }

    public void SetMessageEnvelope(Type envelopeType)
    {
        if (envelopeType.GetGenericTypeDefinition() != typeof(IMessageEnvelope<>))
            throw new ArgumentException("Envelope type must be of type IMessageEnvelope<>", nameof(envelopeType));

        Services.Configure<EventBusInfo>(o =>
        {
           o.MessageEnvelopeType = envelopeType;
        });
    }
}