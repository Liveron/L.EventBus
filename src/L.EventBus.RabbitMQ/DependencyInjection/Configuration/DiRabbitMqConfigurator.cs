using L.EventBus.Abstractions;
using L.EventBus.Abstractions.Filters;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration.Exchange;
using L.EventBus.RabbitMQ.Filters;
using L.EventBus.RabbitMQ.Filters.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace L.EventBus.RabbitMQ.DependencyInjection.Configuration;

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

    public void AddConsumeFilter<TFilter>() where TFilter 
        : class, IRabbitMqConsumeFilter
    {
        services.AddTransient<IRabbitMqConsumeFilter, TFilter>();
    }

    public void AddPublishFilter<TFilter>() where TFilter 
        : class, IRabbitMqPublishFilter
    {
        services.AddTransient<IRabbitMqPublishFilter, TFilter>();
    }

    public void AddSubscription<TEvent, TEventHandler>(string queue) 
        where TEventHandler : class, IEventHandler<TEvent>
    {
        services.AddKeyedTransient<IEventHandler, TEventHandler>(typeof(TEvent));

        services.Configure<EventBusInfo>(o =>
        {
            o.EventTypes[typeof(TEvent).Name] = typeof(TEvent);
        });
        services.Configure<RabbitMqEventBusConfiguration>(o =>
        {
            o.QueueSubscriptions.Add(queue);
        });
    }

    public void SetMessageSerializer<TMessageSerializer>() 
        where TMessageSerializer : class, IRabbitMqMessageSerializerFilter
    {
        RemovePreviousService(typeof(IRabbitMqMessageSerializerFilter));
        services.AddTransient<IRabbitMqMessageSerializerFilter, TMessageSerializer>();
    }

    public void SetMessageDeserializer<TMessageDeserializer>()
        where TMessageDeserializer : class, IRabbitMqMessageDeserializerFilter
    {
        RemovePreviousService(typeof(IRabbitMqMessageDeserializerFilter));
        services.AddTransient<IRabbitMqMessageDeserializerFilter, TMessageDeserializer>();
    }

    public void SetMessageSerializer(Type messageSerializerType)
    {
        if (!typeof(IRabbitMqMessageSerializerFilter).IsAssignableFrom(messageSerializerType) )
            throw new ArgumentException($"{messageSerializerType.Name} must implement {nameof(IRabbitMqMessageSerializerFilter)}");

        Type serviceType;
        if (messageSerializerType.IsGenericTypeDefinition)
        {
            serviceType = typeof(IRabbitMqMessageSerializerFilter<>);
        }
        else if (messageSerializerType.IsGenericType)
        {
            var genericArgument = messageSerializerType.GetGenericArguments()[0];
            serviceType = typeof(IRabbitMqMessageSerializerFilter<>).MakeGenericType(genericArgument);
        }
        else
        {
             serviceType = typeof(IRabbitMqMessageSerializerFilter);
        }

        RemovePreviousService(serviceType);
        services.AddTransient(serviceType, messageSerializerType);
    }

    private void RemovePreviousService(Type serviceType)
    {
        var previousService = services.FirstOrDefault(d => d.ServiceType == serviceType);
        if (previousService is not null)
            services.Remove(previousService);
    }
}