using L.EventBus.Abstractions.Context;

namespace L.EventBus.Abstractions.Filters;

public delegate Task PublishDelegate(IPublishContext context);
