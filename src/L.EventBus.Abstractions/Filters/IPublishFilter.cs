using L.Pipes.Abstractions;

namespace L.EventBus.Abstractions.Filters;

public interface IPublishFilter<TContext> : IFilter<TContext> 
    where TContext : IPipeContext;
