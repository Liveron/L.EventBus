using L.EventBus.DependencyInjection.Configuration;

namespace L.EventBus.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IDiEventBusConfigurator AddEventBus(
        this IServiceCollection services, Action<IDiEventBusConfigurator>? configure = null)
    {
        var configurator = new DiEventBusConfigurator(services);
        configure?.Invoke(configurator);

        return configurator;
    }
}