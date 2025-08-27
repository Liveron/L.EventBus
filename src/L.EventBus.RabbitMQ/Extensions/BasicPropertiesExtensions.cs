using RabbitMQ.Client;
using System.Buffers.Text;
using System.Text;

namespace L.EventBus.RabbitMQ.Extensions;

public static class BasicPropertiesExtensions
{
    public static void SetEventName(this BasicProperties properties, string name)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers.SetEventName(name);
    }

    public static string GetEventName(this IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
            return string.Empty;

        if (!properties.Headers.TryGetValue(Headers.EventName, out var value))
            return string.Empty;

        if (value is not byte[] bytes)
            return string.Empty;

        return Encoding.UTF8.GetString(bytes);
    }
}