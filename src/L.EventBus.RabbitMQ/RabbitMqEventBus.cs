using System.Text;
using System.Text.Json;
using L.EventBus.Abstractions;
using L.EventBus.DependencyInjection.Configuration;
using L.EventBus.RabbitMQ.Configuration;
using L.EventBus.RabbitMQ.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace L.EventBus.RabbitMQ;

public sealed class RabbitMqEventBus : IHostedService, IEventBus, IAsyncDisposable, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly RabbitMqEventBusConfiguration _rabbitMqConfiguration;
    private readonly EventBusInfo _eventBusInfo;
    private readonly ILogger? _logger;

    private IConnection? _rabbitMqConnection;
    private IChannel? _consumerChannel;

    // ReSharper disable once ConvertToPrimaryConstructor
    public RabbitMqEventBus(IServiceProvider services, IOptions<RabbitMqEventBusConfiguration> config,
        IOptions<EventBusInfo> subscriptionsInfo, ILogger<RabbitMqEventBus>? logger = null)
    {
        _services = services;
        _rabbitMqConfiguration = config.Value;
        _eventBusInfo = subscriptionsInfo.Value;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : notnull
    {
        if (!_rabbitMqConfiguration.MessageConfigurations.TryGetValue(typeof(TEvent), out var messageConfiguration))
            throw new InvalidOperationException("There is no routing key for such event type.");

        if (_rabbitMqConnection is null)
            throw new InvalidOperationException("RabbitMQ connection is not open.");

        await using var channel = await _rabbitMqConnection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: messageConfiguration.Exchange, type: ExchangeType.Topic);

        var body = SerializeMessage(@event);

        var properties = CreateProperties(typeof(TEvent).Name);

        await channel.BasicPublishAsync(
            exchange: messageConfiguration.Exchange,
            routingKey: messageConfiguration.RoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body);
    }

    private static BasicProperties CreateProperties(string eventName)
    {
        var properties = new BasicProperties();
        properties.SetEventName(eventName);
        properties.CorrelationId = Guid.NewGuid().ToString();
        return properties;
    }

    private static byte[] SerializeMessage<TEvent>(TEvent @event) where TEvent : notnull
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType());
    }

    private async Task OnMessageReceived(object _, BasicDeliverEventArgs args)
    {
        var eventName = args.BasicProperties.GetEventName();
        if (eventName is null)
        {
            _logger?.LogWarning("Unable to get event name header from message");
            return;
        }

        var message = Encoding.UTF8.GetString(args.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        if (_consumerChannel is null)
            throw new InvalidOperationException("Consumer channel instance is null");

        await _consumerChannel.BasicAckAsync(args.DeliveryTag, multiple: false);
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (!_eventBusInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            _logger?.LogWarning("Unable to resolve event type for event name {EventName}", eventType);
            return;
        }

        var @event = DeserializeMessage(message, eventType);

        await using var scope = _services.CreateAsyncScope();

        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType))
        {
            await handler.HandleAsync(@event);
        }
    }

    private object DeserializeMessage(string message, Type eventType)
    {
        if (_eventBusInfo.MessageEnvelopeType is not null)
        {
            var envelopeType = _eventBusInfo.MessageEnvelopeType.MakeGenericType(eventType);
            return JsonSerializer.Deserialize(message, envelopeType)!;
        }

        return JsonSerializer.Deserialize(message, eventType)!;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger?.LogInformation("Starting RabbitMQ connection on a background thread");

            _rabbitMqConnection = _services.GetRequiredService<IConnection>();
            _consumerChannel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken: stoppingToken);

            foreach (var config in _rabbitMqConfiguration.ExchangeConfigurations)
            {
                await _consumerChannel.ExchangeDeclareAsync(
                    exchange: config.Name,
                    type: config.Type,
                    durable: true,
                    cancellationToken: stoppingToken);
            }

            foreach (var config in _rabbitMqConfiguration.QueueConfigurations)
            {
                await _consumerChannel.QueueDeclareAsync(
                    queue: config.Name,
                    autoDelete: false,
                    durable: true,
                    cancellationToken: stoppingToken);

                await _consumerChannel.QueueBindAsync(
                    queue: config.Name,
                    exchange: config.Exchange,
                    routingKey: config.RoutingKey,
                    cancellationToken: stoppingToken);
            }

            foreach (var subscription in _rabbitMqConfiguration.QueueSubscriptions)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
                consumer.ReceivedAsync += OnMessageReceived;

                await _consumerChannel.BasicConsumeAsync(
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
        if (_consumerChannel != null) 
            await _consumerChannel.DisposeAsync();
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
    }
}