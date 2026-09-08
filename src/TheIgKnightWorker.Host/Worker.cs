using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;
using TheIgKnightWorker.Infrastructure.Messaging.Worker;

namespace TheIgKnightWorker;

public sealed class Worker : BackgroundService
{
    private readonly QueueMessageReceiver _queueMessageReceiver;
    private readonly WorkerExecutor _workerExecutor;

    public Worker(QueueMessageReceiver queueMessageReceiver, WorkerExecutor workerExecutor)
    {
        _queueMessageReceiver = queueMessageReceiver;
        _workerExecutor = workerExecutor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _queueMessageReceiver.StartAsync(
                _workerExecutor.HandleAsync,
                stoppingToken);

            await Task.Delay(
                Timeout.InfiniteTimeSpan,
                stoppingToken);
        }
        catch (OperationCanceledException) when (
            stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            await _queueMessageReceiver.StopAsync();
        }
    }
}