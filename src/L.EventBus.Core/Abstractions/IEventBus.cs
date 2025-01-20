namespace L.EventBus.Core.Abstractions;

public interface IEventBus
{
    Task PublishAsync();
}
