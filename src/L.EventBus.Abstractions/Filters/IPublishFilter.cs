using L.EventBus.Abstractions.Context;

namespace L.EventBus.Abstractions.Filters;

public interface IPublishFilter
{
    Task PublishAsync(IPublishContext context, PublishDelegate next);
}
