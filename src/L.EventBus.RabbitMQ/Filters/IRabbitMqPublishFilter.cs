using L.EventBus.RabbitMQ.Context;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Filters;

public interface IRabbitMqPublishFilter : IFilter<RabbitMqPublishContext>;
