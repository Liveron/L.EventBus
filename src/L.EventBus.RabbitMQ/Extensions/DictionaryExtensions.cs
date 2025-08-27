namespace L.EventBus.RabbitMQ.Extensions;

public static class DictionaryExtensions
{
    public static string GetEventName(this IDictionary<string, object?> headers)
    {
        if (!headers.TryGetValue(Headers.EventName, out var value))
            return string.Empty;
        if (value is not string name)
            return string.Empty;
        return name;
    }

    public static void SetEventName(this IDictionary<string, object?> headers, string eventName)
    {
        headers[Headers.EventName] = eventName;
    }
}
