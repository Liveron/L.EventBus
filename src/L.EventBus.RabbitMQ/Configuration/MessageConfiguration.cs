namespace L.EventBus.RabbitMQ.Configuration;

public record MessageConfiguration(string Exchange, string RoutingKey);