using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using L.EventBus.DependencyInjection;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System.Text.Json;

namespace L.EventBus.RabbitMQ.IntegrationTests;

[Collection(CollectionDefinition.Name)]
public sealed class DiConfigurationTests(Fixture fixture)
{
    private const string ExchangeName = "exchange.test";
    private const string TypeOfExchange = ExchangeType.Topic;
    private const string QueueName = "queue.test";
    private const string QueueRoutingKey = "message.*";
    private const string VHost = "/";
    private const string MessageRoutingKey = "message.test";
    private const string TestMessageContent = "Test"; 

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

    [Fact]
    public async Task SetMessage_ShouldConfigureMessageForPublish()
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


                    exchangeConfig.SetMessage<TestMessage>(MessageRoutingKey);
                });
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = ActivatorUtilities.CreateInstance<RabbitMqEventBus>(provider);
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        // Assert
        var queue = await fixture.ManagementClient.GetQueueAsync(VHost, QueueName);

        //var connection = provider.GetRequiredService<IConnection>();
        //await connection.AbortAsync();

        var messages = await fixture.ManagementClient.GetMessagesFromQueueAsync(VHost, QueueName, 
            new GetMessagesFromQueueInfo(1, AckMode.AckRequeueFalse));

        Assert.NotNull(messages);
        Assert.NotEmpty(messages);

        var message = messages[0];
        var deserializedMessage = JsonSerializer.Deserialize<EventEnvelope<TestMessage>>(message.Payload);

        Assert.Equal(TestMessageContent, deserializedMessage!.Payload.Text);
    }
    private sealed record TestMessage(string Text);
}