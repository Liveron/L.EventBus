namespace L.EventBus.DependencyInjection.Configuration;

public class EventBusInfo
{
    public Dictionary<string, Type> EventTypes { get; } = [];
    public Type? MessageEnvelopeType { get; set; }
}