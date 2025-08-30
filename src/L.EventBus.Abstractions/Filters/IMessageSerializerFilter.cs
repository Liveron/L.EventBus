using L.Pipes.Abstractions;

namespace L.EventBus.Abstractions.Filters;

public interface IMessageSerializerFilter : IFilter;

public interface IMessageSerializerFilter<TContext> 
    : IMessageSerializerFilter, IFilter<TContext> where TContext : IPipeContext;