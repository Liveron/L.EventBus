using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace L.EventBus.RabbitMQ.IntegrationTests;

public static class TestUtils
{
    private const int NotFoundReplyCode = 404;

    public static async Task<bool> ExchangeExistsAsync(IConnection connection, string exchangeName)
    {
        return await CheckResourceExistsAsync(connection,
            channel => channel.ExchangeDeclarePassiveAsync(exchangeName));
    }

    public static async Task<bool> QueueExistsAsync(IConnection connection, string queueName)
    {
        return await CheckResourceExistsAsync(connection,
            channel => channel.QueueDeclarePassiveAsync(queueName));
    }

    private static async Task<bool> CheckResourceExistsAsync(
        IConnection connection, Func<IChannel, Task> checkAction)
    {
        try
        {
            await using var channel = await connection.CreateChannelAsync();
            await checkAction(channel);
            return true;
        }
        catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == NotFoundReplyCode)
        {
            return false;
        }
    }
}