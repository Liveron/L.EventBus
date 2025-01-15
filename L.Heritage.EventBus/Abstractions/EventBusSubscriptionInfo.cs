namespace L.Heritage.EventBus.Abstractions;

public class EventBusSubscriptionInfo
{
    public Dictionary<string, Type> EventTypes { get; } = [];
}
