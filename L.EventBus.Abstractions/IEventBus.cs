namespace L.EventBus.Abstractions;
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event);
}