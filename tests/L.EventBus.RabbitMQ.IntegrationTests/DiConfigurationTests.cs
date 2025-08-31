using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using L.EventBus.Abstractions;
using L.EventBus.Abstractions.Configuration;
using L.EventBus.Abstractions.Filters;
using L.EventBus.DependencyInjection;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Filters;
using L.EventBus.RabbitMQ.Filters.Serialization;
using L.Pipes.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
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
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
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
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
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
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
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
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        // Assert
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
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        await Task.Delay(10000);

        // Assert
        var counter = provider.GetRequiredService<TestCounter>();
        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public async Task SetSerializer_ShouldSerializeMessage()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString, mqConfigurator =>
            {
                mqConfigurator.SetMessageSerializer<TestSerializer>();

                mqConfigurator.SetExchange(TypeOfExchange, ExchangeName, exchangeConfig =>
                {
                    exchangeConfig.SetQueue(QueueName, QueueRoutingKey);
                    exchangeConfig.SetMessage<TestMessage>(MessageRoutingKey);
                });
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        var messages = await fixture.ManagementClient.GetMessagesFromQueueAsync(VHost, QueueName,
            new GetMessagesFromQueueInfo(1, AckMode.AckRequeueFalse));

        // Assert
        Assert.NotNull(messages);
        Assert.NotEmpty(messages);

        var message = messages[0];
        var payloadString = message.Payload;
        var envelope = JsonSerializer.Deserialize<TestEnvelope<TestMessage>>(payloadString);

        Assert.NotNull(envelope);
        Assert.IsType<TestMessage>(envelope.Payload);

        var nonGenericEnvelope = envelope as TestEnvelope;

        Assert.NotNull(nonGenericEnvelope);
        Assert.IsType<TestMessage>(nonGenericEnvelope.Payload);
    }
    [Fact]
    public async Task SetOpenGenericSerializer_ShouldSerializeMessage()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEventBus(config =>
        {
            var connectionString = fixture.RabbitMqContainer.GetConnectionString();
            config.UseRabbitMq(connectionString, mqConfigurator =>
            {
                mqConfigurator.SetMessageSerializer(typeof(TestOpenGenericSerializer<>));
                mqConfigurator.SetExchange(TypeOfExchange, ExchangeName, exchangeConfig =>
                {
                    exchangeConfig.SetQueue(QueueName, QueueRoutingKey);
                    exchangeConfig.SetMessage<TestMessage>(MessageRoutingKey);
                });
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
        await eventBus.StartAsync(CancellationToken.None);
        await eventBus.PublishAsync(new TestMessage(TestMessageContent));

        var messages = await fixture.ManagementClient.GetMessagesFromQueueAsync(VHost, QueueName,
            new GetMessagesFromQueueInfo(1, AckMode.AckRequeueFalse));

        // Assert
        Assert.NotNull(messages);
        Assert.NotEmpty(messages);

        var message = messages[0];
        var payloadString = message.Payload;
        var envelope = JsonSerializer.Deserialize<TestEnvelope<TestMessage>>(payloadString);

        Assert.NotNull(envelope);
        Assert.IsType<TestMessage>(envelope.Payload);

        var nonGenericEnvelope = envelope as TestEnvelope;

        Assert.NotNull(nonGenericEnvelope);
        Assert.IsType<TestMessage>(nonGenericEnvelope.Payload);
    }


    [Fact]
    public async Task SetDeserializer_CorrectlyDeserializeMessage()
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
                mqConfigurator.SetMessageSerializer<TestSerializer>();
                mqConfigurator.SetMessageDeserializer<TestDeserializer>();

                mqConfigurator.SetExchange(TypeOfExchange, ExchangeName, exchangeConfig =>
                {
                    exchangeConfig.SetQueue(QueueName, QueueRoutingKey);
                    exchangeConfig.SetMessage<TestMessage>(MessageRoutingKey);
                });

                mqConfigurator.AddSubscription<TestMessage, TestEventHandler>(QueueName);
            });
        });
        await using var provider = services.BuildServiceProvider();
        await using var eventBus = provider.GetRequiredService<IRabbitMqEventBus>();
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

    private sealed class TestSerializer : IRabbitMqMessageSerializerFilter
    {
        public async Task HandleAsync(RabbitMqPublishContext context, FilterDelegate next)
        {
            var envelope = new TestEnvelope<TestMessage>((context.Payload as TestMessage)!);
            var messageString = JsonSerializer.Serialize(envelope);
            context.Payload = Encoding.UTF8.GetBytes(messageString);
            await next(context);
        }
    }

    private sealed class TestOpenGenericSerializer<TPayload> : IRabbitMqMessageSerializerFilter<TPayload>  where TPayload : notnull
    {
        public async Task HandleAsync(RabbitMqPublishContext<TPayload> context, FilterDelegate next)
        {
            var envelope = new TestEnvelope<TPayload>(context.Payload);
            var messageString = JsonSerializer.Serialize(envelope);
            var nextContext = new RabbitMqPublishContext(Encoding.UTF8.GetBytes(messageString),
                context.RoutingKey, context.Exchange, context.EventName)
            {
                Headers = context.Headers,
            };
            await next(nextContext);
        }
    }

    private sealed class TestDeserializer(IOptions<EventBusInfo> info) : IRabbitMqMessageDeserializerFilter
    {
        private readonly EventBusInfo _eventBusInfo = info.Value;

        public async Task HandleAsync(RabbitMqConsumeContext<ReadOnlyMemory<byte>> context, FilterDelegate next)
        {
            if (string.IsNullOrWhiteSpace(context.EventName))
                return;

            var messageString = Encoding.UTF8.GetString(context.Payload.Span);
            if (!_eventBusInfo.EventTypes.TryGetValue(context.EventName, out var type))
                return;

            var envelopeType = typeof(TestEnvelope<>).MakeGenericType(type);
            var message = JsonSerializer.Deserialize(messageString, envelopeType);
            if (message is not TestEnvelope envelope)
                return;

            var nextContext = new RabbitMqConsumeContext(envelope.Payload, context.DeliveryTag)
            {
                Headers = context.Headers,
            };
            await next(nextContext);
        }
    }

    private class TestEnvelope(object payload)
    {
        public string EnvelopeProp { get; set; } = "Hello";
        public object Payload { get; set; } = payload;
    }

    private sealed class TestEnvelope<TPayload>(TPayload payload) : TestEnvelope(payload)
    {
        public new TPayload Payload { get; set; } = payload;

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

        public async Task HandleAsync(RabbitMqPublishContext context, FilterDelegate next)
        {
            context.Payload = new TestMessage(FilterKey);

            await next(context);
        }
    }

    private sealed class SecondTestPublishFilter : IRabbitMqPublishFilter
    {
        public const string FilterKey = "Second Filter Applied";

        public async Task HandleAsync(RabbitMqPublishContext context, FilterDelegate next)
        {
            context.Payload = new TestMessage(FilterKey);
            await next(context);
        }
    }

    private sealed record TestMessage(string Text);
}