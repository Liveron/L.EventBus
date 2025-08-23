namespace L.EventBus.Abstractions.Context;

public interface IConsumeContext
{
    public object Payload { get; set; }
}
