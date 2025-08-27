using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.Context;
using L.EventBus.RabbitMQ.Extensions;
using L.EventBus.RabbitMQ.Filters;
using L.EventBus.RabbitMQ.Filters.MessageHandling;
using L.EventBus.RabbitMQ.Filters.MessagePublishing;
using L.EventBus.RabbitMQ.Filters.Serialization;
using L.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace L.EventBus.RabbitMQ;

public sealed class RabbitMqEventBus : IRabbitMqEventBus
{
    private readonly IServiceProvider _services;
    private readonly RabbitMqEventBusConfiguration _rabbitMqConfiguration;
    private readonly ILogger? _logger;

    public IChannel? ConsumerChannel { get; private set; }

    // ReSharper disable once ConvertToPrimaryConstructor
    public RabbitMqEventBus(IServiceProvider services, IOptions<RabbitMqEventBusConfiguration> config,
        ILogger<RabbitMqEventBus>? logger = null)
    {
        _services = services;
        _rabbitMqConfiguration = config.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : notnull
    {
        await using var scope = _services.CreateAsyncScope();
        var filters = scope.ServiceProvider.GetServices<IRabbitMqPublishFilter>();
        var serializer = scope.ServiceProvider.GetRequiredService<IRabbitMqMessageSerializerFilter>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRabbitMqMessagePublisherFilter>();

        var context = CreatePublishContext(@event);
        await Pipe.ExecuteFiltersAsync([.. filters, serializer, publisher], context);
    }

    private RabbitMqPublishContext CreatePublishContext<TEvent>(TEvent @event) where TEvent : notnull
    {
        if (!_rabbitMqConfiguration.MessageConfigurations.TryGetValue(@event.GetType(), out var messageConfiguration))
            throw new InvalidOperationException("There is not such ");

        return new RabbitMqPublishContext(@event, messageConfiguration.RoutingKey, 
            messageConfiguration.Exchange, @event.GetType().Name);
    }

    private async Task OnMessageReceived(object _, BasicDeliverEventArgs args)
    {
        await using var scope = _services.CreateAsyncScope();
        var deserializer = scope.ServiceProvider.GetRequiredService<IRabbitMqMessageDeserializerFilter>();
        var filters = scope.ServiceProvider.GetServices<IRabbitMqConsumeFilter>();
        var messageHandler = scope.ServiceProvider.GetRequiredService<IRabbitMqMessageHandlerFilter>();

        var context = CreateConsumeContext(args);
        await Pipe.ExecuteFiltersAsync([deserializer, .. filters, messageHandler], context);
    }

    private static RabbitMqConsumeContext<ReadOnlyMemory<byte>> CreateConsumeContext(
        BasicDeliverEventArgs args)
    {
        var eventName = args.BasicProperties.GetEventName();

        return new RabbitMqConsumeContext<ReadOnlyMemory<byte>>(
            args.Body, args.DeliveryTag, eventName, args.BasicProperties.Headers);
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger?.LogInformation("Starting RabbitMQ connection on a background thread");

            var connection = _services.GetRequiredService<IConnection>();
            if (!connection.IsOpen)
            {
                _logger?.LogWarning("RabbitMQ connection is not open.");
                return;
            }

            ConsumerChannel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

            foreach (var config in _rabbitMqConfiguration.ExchangeConfigurations)
            {
                await ConsumerChannel.ExchangeDeclareAsync(
                    exchange: config.Name,
                    type: config.Type,
                    durable: true,
                    cancellationToken: stoppingToken);
            }

            foreach (var config in _rabbitMqConfiguration.QueueConfigurations)
            {
                await ConsumerChannel.QueueDeclareAsync(
                    queue: config.Name,
                    autoDelete: false,
                    durable: true,
                    cancellationToken: stoppingToken,
                    exclusive: false);

                await ConsumerChannel.QueueBindAsync(
                    queue: config.Name,
                    exchange: config.Exchange,
                    routingKey: config.RoutingKey,
                    cancellationToken: stoppingToken);
            }

            foreach (var subscription in _rabbitMqConfiguration.QueueSubscriptions)
            {
                var consumer = new AsyncEventingBasicConsumer(ConsumerChannel);
                consumer.ReceivedAsync += OnMessageReceived;

                await ConsumerChannel.BasicConsumeAsync(
                    queue: subscription,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);
            }

        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error starting RabbitMQ connection");
        }
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (ConsumerChannel is not null)
            await ConsumerChannel.DisposeAsync();
    }

    public void Dispose()
    {
        ConsumerChannel?.Dispose();
    }
}