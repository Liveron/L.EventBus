using L.Pipes.Abstractions;

namespace L.EventBus.Abstractions.Filters;

public interface IConsumeFilter<TContext> : IFilter<TContext> 
    where TContext : IPipeContext;
