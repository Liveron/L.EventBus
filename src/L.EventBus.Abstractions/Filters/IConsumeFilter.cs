using L.EventBus.Abstractions.Context;

namespace L.EventBus.Abstractions.Filters;

public interface IConsumeFilter
{
    public Task InvokeAsync(IConsumeContext context, FilterDelegate next);
}
