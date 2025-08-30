using L.Pipes.Abstractions;

namespace L.EventBus.Abstractions.Filters;

public interface IMessageDeserializerFilter : IFilter;

public interface IMessageDeserializerFilter<TContext> 
    : IMessageDeserializerFilter, IFilter<TContext> where TContext : IPipeContext;
