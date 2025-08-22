using EasyNetQ.Management.Client;
using L.EventBus.DependencyInjection;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.IntegrationTests;

[Collection(CollectionDefinition.Name)]
public sealed class DiConfigurationTests(Fixture fixture)
{
    private const string ExchangeName = "exchange.test";
    private const string TypeOfExchange = ExchangeType.Topic;
    private const string QueueName = "queue.test";
    private const string QueueRoutingKey = "message.*";
    private const string VHost = "/";

    [Fact]
    public void AddRabbitMq_ShouldCreateValidConnection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString);
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var connection = provider.GetService<IConnection>();
        
        Assert.NotNull(connection);
        Assert.True(connection.IsOpen);
    }

    [Fact]
    public async Task SetExchange_ShouldCreateExchange()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString, mqConfigurator =>
            {
                mqConfigurator.SetExchange(TypeOfExchange, ExchangeName);
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = ActivatorUtilities.CreateInstance<RabbitMqEventBus>(provider);
        await eventBus.StartAsync(CancellationToken.None);

        // Assert
        var exchanges = await fixture.ManagementClient.GetExchangesAsync(VHost);
        var createdExchange = exchanges.FirstOrDefault(e => e.Name == ExchangeName);

        Assert.NotNull(createdExchange);
        Assert.Equal(TypeOfExchange, createdExchange.Type);
    }

    [Fact]
    public async Task SetQueue_ShouldCreateQueue()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString, mqConfigurator =>
            {
                mqConfigurator.SetExchange(TypeOfExchange, ExchangeName, exchangeConfig =>
                {
                    exchangeConfig.SetQueue(QueueName, QueueRoutingKey);
                });
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = ActivatorUtilities.CreateInstance<RabbitMqEventBus>(provider);
        await eventBus.StartAsync(CancellationToken.None);

        // Assert
        var queues = await fixture.ManagementClient.GetQueuesAsync();
        var createdQueue = queues.FirstOrDefault(q => q.Name == QueueName);

        Assert.NotNull(createdQueue);

        var bindings = await fixture.ManagementClient.GetBindingsForQueueAsync(
            VHost, QueueName);

        Assert.NotEmpty(bindings);
        var createdBinding = bindings.FirstOrDefault(b => b.RoutingKey == QueueRoutingKey);
        Assert.NotNull(createdBinding);
    }
}