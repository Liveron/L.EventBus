using RabbitMQ.Client;

namespace L.EventBus.RabbitMQ.Extensions;

public static class BasicPropertiesExtensions
{
    public static void SetEventName(this BasicProperties properties, string name)
    {
        properties.Headers ??= new Dictionary<string, object?>();
        properties.Headers[Headers.EventName] = name;
    }

    public static string? GetEventName(this IReadOnlyBasicProperties properties)
    {
        if (properties.Headers is null)
            return null;

        if (!properties.Headers.TryGetValue(Headers.EventName, out var value))
            return null;

        return value as string;
    }
}