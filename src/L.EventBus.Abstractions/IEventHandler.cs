namespace L.EventBus.Abstractions;

public interface IEventHandler<in TEvent> : IEventHandler
{
    Task HandleAsync(TEvent @event);
    Task IEventHandler.HandleAsync(object @event)
        => HandleAsync((TEvent)@event);
}

public interface IEventHandler
{
    Task HandleAsync(object @event);
}