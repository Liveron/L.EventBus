﻿using L.Heritage.EventBus.Abstractions;

namespace L.Heritage.EventBus.Extensions.DependencyInjection;

public static class EventBusBuilderExtensions
{
    public static IEventBusBuilder AddSubscription<TIntegrationEvent, TIntegrationEventHandler>(
        this IEventBusBuilder eventBusBuilder) 
        where TIntegrationEvent : IntegrationEvent
        where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
    {
        eventBusBuilder.Services.AddKeyedTransient<IIntegrationEventHandler>(typeof(TIntegrationEvent));

        eventBusBuilder.Services.Configure<EventBusSubscriptionInfo>(o =>
        {
            o.EventTypes[typeof(TIntegrationEvent).Name] = typeof(TIntegrationEventHandler);
        });

        return eventBusBuilder;
    }
}