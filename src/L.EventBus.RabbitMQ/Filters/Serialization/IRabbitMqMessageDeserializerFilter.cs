using L.EventBus.Abstractions.Filters;
using L.EventBus.RabbitMQ.Context;

namespace L.EventBus.RabbitMQ.Filters.Serialization;

public interface IRabbitMqMessageDeserializerFilter 
    : IMessageDeserializerFilter<RabbitMqConsumeContext<ReadOnlyMemory<byte>>>;
