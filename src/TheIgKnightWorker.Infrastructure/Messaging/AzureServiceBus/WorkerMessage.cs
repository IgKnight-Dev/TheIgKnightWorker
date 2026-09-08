using System.Text.Json;

namespace TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

public class WorkerMessage
{
    public Guid MessageId { get; init; }

    public string MessageType { get; init; } = string.Empty;

    public Guid UserId { get; init; }

    public Guid? CorrelationId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public JsonElement Data { get; init; }
}

public sealed class WorkerMessage<T> : WorkerMessage
{
    public T Data { get; init; } = default!;
}