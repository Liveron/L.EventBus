

using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqConsumeContext(
    object payload, 
    ulong deliveryTag,
    string eventName,
    IDictionary<string, object?>? headers) 
    : IPipeContext
{
    public IDictionary<string, object?>? Headers { get; } = headers;
    public object Payload { get; set; } = payload;
    public ulong DeliveryTag => deliveryTag;
    public string EventName { get; set; } = eventName;
}
public class RabbitMqConsumeContext<TPayload>(
    TPayload payload, 
    ulong deliveryTag,
    string eventName,
    IDictionary<string, object?>? headers)
    : RabbitMqConsumeContext(payload, deliveryTag, eventName, headers), 
    IPipeContext<TPayload> where TPayload : notnull
{
    public new TPayload Payload { get; set; } = payload;
}