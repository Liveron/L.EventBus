namespace L.Heritage.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync();
}
