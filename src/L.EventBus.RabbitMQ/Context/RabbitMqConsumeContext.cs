using L.EventBus.RabbitMQ.Extensions;
using L.Pipes.Abstractions;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqConsumeContext(object payload, ulong deliveryTag) : IPipeContext
{
    public IDictionary<string, object?> Headers { get; init; } = new Dictionary<string, object?>();
    public object Payload { get; set; } = payload;
    public ulong DeliveryTag { get; set; } = deliveryTag;
    public string EventName
    {
        get => Headers is null ? string.Empty : Headers.GetEventName();
        set => Headers.SetEventName(value);
    }
}
public class RabbitMqConsumeContext<TPayload>(TPayload payload, ulong deliveryTag)
    : RabbitMqConsumeContext(payload, deliveryTag), IPipeContext<TPayload> where TPayload : notnull
{
    public new TPayload Payload { get; set; } = payload;
}