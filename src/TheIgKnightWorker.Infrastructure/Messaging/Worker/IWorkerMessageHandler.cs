using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

namespace TheIgKnightWorker.Infrastructure.Messaging.Worker;


public interface IWorkerMessageHandler
{
    string MessageType { get; }

    Task HandleAsync(WorkerMessage message, CancellationToken cancellationToken = default);
}
