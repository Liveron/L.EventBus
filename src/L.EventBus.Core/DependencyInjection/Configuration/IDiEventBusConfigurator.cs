using L.EventBus.Abstractions.Configuration;

namespace L.EventBus.DependencyInjection.Configuration;

public interface IDiEventBusConfigurator : IEventBusConfigurator
{
    public IServiceCollection Services { get; }
}