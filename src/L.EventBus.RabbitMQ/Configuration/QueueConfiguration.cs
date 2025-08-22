namespace L.EventBus.RabbitMQ.Configuration;

public sealed record QueueConfiguration(string Name, string Exchange, string RoutingKey);