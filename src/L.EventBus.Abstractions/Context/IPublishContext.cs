namespace L.EventBus.Abstractions.Context;

public interface IPublishContext
{
    public object Payload { get; set; }
}
