using System.Text.Json;

namespace TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

public sealed class ServiceBusMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public BinaryData Serialize<T>(T message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return BinaryData.FromObjectAsJson(message, JsonOptions);
    }

    public T Deserialize<T>(BinaryData message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var result = message.ToObjectFromJson<T>(JsonOptions);

        return result
            ?? throw new InvalidOperationException(
                $"Unable to deserialize Service Bus message to {typeof(T).Name}.");
    }
}
