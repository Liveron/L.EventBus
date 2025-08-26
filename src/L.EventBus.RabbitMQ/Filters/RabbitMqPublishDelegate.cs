using L.EventBus.RabbitMQ.Context;

namespace L.EventBus.RabbitMQ.Filters;

public delegate Task RabbitMqPublishDelegate(IRabbitMqPublishContext context);
