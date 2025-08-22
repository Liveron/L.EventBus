using EasyNetQ.Management.Client;
using Testcontainers.RabbitMq;

namespace L.EventBus.RabbitMQ.IntegrationTests;

public static class CollectionDefinition
{
    public const string Name = "Test Collection";
}

[CollectionDefinition(CollectionDefinition.Name)]
public sealed class CollectionFixture : ICollectionFixture<Fixture>;

public sealed class Fixture : IAsyncLifetime
{
    private const string RabbitMqUser = "guest";
    private const string RabbitMqUserPassword = "guest";

    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:4.1.3-management-alpine")
        .WithPortBinding(15672, 15672)
        .WithUsername(RabbitMqUser)
        .WithPassword(RabbitMqUserPassword)
        .Build();
    public IManagementClient ManagementClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await RabbitMqContainer.StartAsync();
        ManagementClient = new ManagementClient(new Uri("http://localhost:15672"),RabbitMqUser, RabbitMqUserPassword);
    }

    public async Task DisposeAsync()
    {
        await RabbitMqContainer.StopAsync();
        await RabbitMqContainer.DisposeAsync();
    }
}