using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

namespace TheIgKnightWorker.Infrastructure.Messaging.Worker;

public sealed class DebugWorkerMessageHandler : IWorkerMessageHandler
{
    public string MessageType => "Debug";

    public Task HandleAsync(
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(
            $"DEBUG MESSAGE HANDLED: {message.MessageId}");

        Console.WriteLine(
            $"DEBUG MESSAGE TYPE: {message.MessageType}");

        Console.WriteLine(
            $"DEBUG USER ID: {message.UserId}");

        return Task.CompletedTask;
    }
}