using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using L.EventBus.Extensions;
using L.EventBus.Abstractions;
using L.EventBus.Events;

namespace L.EventBus.Core.Tests.EventBusBuilderExtensions;

public class AddSubscriptionTests
{
    private readonly Mock<IEventBusBuilder> _mockEventBusBuilder;
    private readonly ServiceCollection _services;

    public AddSubscriptionTests()
    {
        _services = new ServiceCollection();
        _mockEventBusBuilder = new Mock<IEventBusBuilder>();
        _mockEventBusBuilder.Setup(builder => builder.Services).Returns(_services);
    }

    [Fact]
    public void ShouldRegisterIntegrationEventHandlerAsKeyedService()
    {
        // Arrange
        var eventBusBuilder = _mockEventBusBuilder.Object;

        // Act
        eventBusBuilder.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        var service = _services.BuildServiceProvider()
            .GetKeyedService<IIntegrationEventHandler>(typeof(TestIntegrationEvent));

        Assert.NotNull(service);
    }

    [Fact]
    public void ShouldRegisterIntegrationEventHandlerAsTransient()
    {
        // Arrange
        var eventBusBuilder = _mockEventBusBuilder.Object;

        // Act
        eventBusBuilder.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        var service = _services.First(sd =>
            sd.ServiceType == typeof(IIntegrationEventHandler) &&
            sd.ServiceKey?.ToString() == typeof(TestIntegrationEvent).ToString());

        // Assert
        Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
    }

    [Fact]
    public void ShouldRegisterSubscriptionInfoAndEventHandlerInIt()
    {
        // Arrange
        var eventBusBuilder = _mockEventBusBuilder.Object;

        // Act
        eventBusBuilder.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        var options = _services.BuildServiceProvider()
            .GetService<IOptions<EventBusSubscriptionInfo>>();

        var subscriptionInfo = options?.Value;

        // Assert
        Assert.Equal(typeof(TestIntegrationEventHandler),
            subscriptionInfo?.EventTypes[typeof(TestIntegrationEvent).Name]);
    }

    [Fact]
    public void ShouldReturnSameEventBusBuilder()
    {
        // Arrange
        var eventBusBuilder = _mockEventBusBuilder.Object;

        // Act
        var result = eventBusBuilder.AddSubscription<TestIntegrationEvent, TestIntegrationEventHandler>();

        // Assert
        Assert.Equal(eventBusBuilder, result);
    }

    private record TestIntegrationEvent : IntegrationEvent { }

    private class TestIntegrationEventHandler : IIntegrationEventHandler<TestIntegrationEvent>
    {
        public Task Handle(TestIntegrationEvent _) => Task.CompletedTask;
    }
}
