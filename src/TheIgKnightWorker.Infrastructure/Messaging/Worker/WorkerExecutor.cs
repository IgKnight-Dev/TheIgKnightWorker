using Microsoft.Extensions.DependencyInjection;
using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

namespace TheIgKnightWorker.Infrastructure.Messaging.Worker;

public sealed class WorkerExecutor
{
    private readonly IServiceProvider _serviceProvider;

    public WorkerExecutor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.MessageType))
        {
            throw new InvalidOperationException(
                "Worker message type is required.");
        }

        var handlers =
            _serviceProvider.GetServices<IWorkerMessageHandler>();

        var handler = handlers.FirstOrDefault(
            x => string.Equals(
                x.MessageType,
                message.MessageType,
                StringComparison.OrdinalIgnoreCase));

        if (handler is null)
        {
            throw new InvalidOperationException(
                $"No worker message handler is registered for message type '{message.MessageType}'.");
        }

        await handler.HandleAsync(
            message,
            cancellationToken);
    }
}