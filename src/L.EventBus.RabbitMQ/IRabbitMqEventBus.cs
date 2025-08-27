using L.EventBus.Abstractions;
using Microsoft.Extensions.Hosting;

namespace L.EventBus.RabbitMQ;

public interface IRabbitMqEventBus : IEventBus, IHostedService, IDisposable, IAsyncDisposable;
