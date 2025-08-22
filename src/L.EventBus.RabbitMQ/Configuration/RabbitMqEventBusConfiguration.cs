namespace L.EventBus.RabbitMQ.Configuration;

public sealed class RabbitMqEventBusConfiguration
{
    public List<ExchangeConfiguration> ExchangeConfigurations { get; } = [];
    public List<QueueConfiguration> QueueConfigurations { get; } = [];
    public Dictionary<Type, MessageConfiguration> MessageConfigurations { get; } = [];
    public List<string> QueueSubscriptions { get; } = [];
}