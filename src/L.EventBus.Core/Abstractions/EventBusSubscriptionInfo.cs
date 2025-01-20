namespace L.EventBus.Core.Abstractions;

public class EventBusSubscriptionInfo
{
    public Dictionary<string, Type> EventTypes { get; } = [];
}
