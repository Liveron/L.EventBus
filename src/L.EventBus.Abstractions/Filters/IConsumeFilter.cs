using L.EventBus.Abstractions.Context;

namespace L.EventBus.Abstractions.Filters;

public interface IConsumeFilter
{
    public Task ConsumeAsync(IConsumeContext context, ConsumeDelegate next);
}
