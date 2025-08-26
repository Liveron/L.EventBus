using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using L.EventBus.Abstractions;
using L.EventBus.Abstractions.Context;
using L.EventBus.Abstractions.Filters;
using L.EventBus.DependencyInjection;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Filters;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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

        var messages = await fixture.ManagementClient.GetMessagesFromQueueAsync(VHost, QueueName, 
            new GetMessagesFromQueueInfo(1, AckMode.AckRequeueFalse));

        Assert.NotNull(messages);
        Assert.NotEmpty(messages);

        var message = messages[0];
        var deserializedMessage = JsonSerializer.Deserialize<TestMessage>(message.Payload);

        Assert.Equal(TestMessageContent, deserializedMessage!.Text);
    }

    [Fact]
    public async Task AddPublishFilter_AppliesFiltersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();


        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString, mqConfigurator =>
            {
                mqConfigurator.AddPublishFilter<FirstTestPublishFilter>();
                mqConfigurator.AddPublishFilter<SecondTestPublishFilter>();

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

        var messages = await fixture.ManagementClient.GetMessagesFromQueueAsync(VHost, QueueName,
            new GetMessagesFromQueueInfo(1, AckMode.AckRequeueFalse));

        Assert.NotNull(messages);
        Assert.NotEmpty(messages);

        var message = messages[0];
        var deserializedMessage = JsonSerializer.Deserialize<TestMessage>(message.Payload);

        Assert.Equal(SecondTestPublishFilter.FilterKey, deserializedMessage!.Text);
    }

    [Fact]
    public async Task AddSubscription_ShouldCreateValidSubscription()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSingleton<TestCounter>();
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
                mqConfigurator.AddSubscription<TestMessage, TestEventHandler>(QueueName);
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = ActivatorUtilities.CreateInstance<RabbitMqEventBus>(provider);
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        await Task.Delay(10000);

        // Assert
        var counter = provider.GetRequiredService<TestCounter>();
        Assert.Equal(1, counter.Count);
    }

    private sealed class TestCounter
    {
        public int Count { get; private set; } = 0;
        public void Increment() => Count++;
    }

    private sealed class TestEventHandler(TestCounter counter) : IEventHandler<TestMessage>
    {
        public Task HandleAsync(TestMessage @event)
        {
            counter.Increment();
            return Task.CompletedTask;
        }
    }

    private sealed class FirstTestPublishFilter : IRabbitMqPublishFilter
    {
        public const string FilterKey = "First Filter Applied";

        public async Task PublishAsync(IRabbitMqPublishContext context, RabbitMqPublishDelegate next)
        {
            context.Payload = new TestMessage(FilterKey);

            await next(context);
        }
    }

    private sealed class SecondTestPublishFilter : IRabbitMqPublishFilter
    {
        public const string FilterKey = "Second Filter Applied";

        public async Task PublishAsync(IRabbitMqPublishContext context, RabbitMqPublishDelegate next)
        {
            context.Payload = new TestMessage(FilterKey);
            await next(context);
        }
    }

    private sealed record TestMessage(string Text);
}