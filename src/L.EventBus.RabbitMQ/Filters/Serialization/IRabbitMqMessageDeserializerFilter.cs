namespace L.EventBus.RabbitMQ.Filters.Serialization;

public interface IRabbitMqMessageDeserializerFilter : IRabbitMqConsumeFilter<ReadOnlyMemory<byte>>;
