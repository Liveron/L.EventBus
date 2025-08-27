using L.Pipes.Abstractions;
using RabbitMQ.Client.Events;

namespace L.EventBus.RabbitMQ.Context;

public class RabbitMqDeserializerContext(BasicDeliverEventArgs payload) 
    : IPipeContext<BasicDeliverEventArgs>
{
    public BasicDeliverEventArgs Payload { get; set; } = payload;
    object IPipeContext.Payload 
    { 
        get => Payload;
        set => Payload = (BasicDeliverEventArgs)value;
    }
}
