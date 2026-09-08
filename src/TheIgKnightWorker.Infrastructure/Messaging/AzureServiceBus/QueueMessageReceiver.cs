using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

public sealed class QueueMessageReceiver : IAsyncDisposable
{
    private readonly ServiceBusClientFactory _clientFactory;
    private readonly AzureServiceBusOptions _options;
    private readonly ILogger<QueueMessageReceiver> _logger;
    private readonly ServiceBusMessageSerializer _messageSerializer;

    private ServiceBusClient? _client;
    private ServiceBusProcessor? _processor;

    private Func<WorkerMessage, CancellationToken, Task>? _messageHandler;

    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

    public QueueMessageReceiver(ServiceBusClientFactory clientFactory, ServiceBusMessageSerializer messageSerializer, IOptions<AzureServiceBusOptions> options, ILogger<QueueMessageReceiver> logger)
    {
        _clientFactory = clientFactory;
        _messageSerializer = messageSerializer;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Starts receiving messages from the configured Azure Service Bus queue.
    /// The supplied callback is invoked for every received message.
    /// </summary>
    public async Task StartAsync(Func<WorkerMessage, CancellationToken, Task> messageHandler, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageHandler);

        await _lifecycleLock.WaitAsync(cancellationToken);

        try
        {
            if (_processor is not null)
            {
                if (_processor.IsProcessing)
                {
                    return;
                }

                throw new InvalidOperationException(
                    "The queue message receiver has already been initialized. " +
                    "Create a new receiver instance if the processor was closed.");
            }

            ValidateConfiguration();

            _messageHandler = messageHandler;

            _client = _clientFactory.CreateClient();

            _processor = _client.CreateProcessor(
                _options.QueueName,
                new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,

                    MaxConcurrentCalls = _options.MaxConcurrentCalls,

                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(
                        _options.MaxAutoLockRenewalDurationMinutes
                    ),

                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync(cancellationToken);
        }
        catch
        {
            await CleanupProcessorAsync();

            throw;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops receiving new messages and waits for currently executing
    /// message handlers to finish.
    /// </summary>
    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken);

        try
        {
            if (_processor is null)
            {
                return;
            }

            if (!_processor.IsProcessing)
            {
                return;
            }

            await _processor.StopProcessingAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Handles every message received by the Service Bus processor.
    /// The actual business processing is delegated to the callback
    /// supplied to StartAsync.
    /// </summary>
    private async Task ProcessMessageAsync(
     ProcessMessageEventArgs args)
    {
        var message = args.Message;

        if (_messageHandler is null)
        {
            throw new InvalidOperationException(
                "No message handler has been configured.");
        }

        try
        {
            var workerMessage =
                _messageSerializer.Deserialize<WorkerMessage>(
                    message.Body);

            await _messageHandler(
                workerMessage,
                args.CancellationToken);

            await args.CompleteMessageAsync(message);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Handles Service Bus processor-level errors.
    /// These are transport/infrastructure errors rather than
    /// business-processing errors.
    /// </summary>
    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine(
            $"[SERVICE BUS ERROR] {args.Exception}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Releases the Service Bus processor and client resources.
    /// </summary>
    private async Task CleanupProcessorAsync()
    {
        if (_processor is not null)
        {
            try
            {
                _processor.ProcessMessageAsync -= ProcessMessageAsync;
                _processor.ProcessErrorAsync -= ProcessErrorAsync;

                await _processor.DisposeAsync();
            }
            catch (Exception)
            {

            }
            finally
            {
                _processor = null;
            }
        }

        if (_client is not null)
        {
            try
            {
                await _client.DisposeAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                _client = null;
            }
        }

        _messageHandler = null;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.FullyQualifiedNamespace))
        {
            throw new InvalidOperationException(
                "Azure Service Bus fully qualified namespace is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.QueueName))
        {
            throw new InvalidOperationException(
                "Azure Service Bus queue name is required.");
        }

        if (_options.MaxConcurrentCalls <= 0)
        {
            throw new InvalidOperationException(
                "Azure Service Bus MaxConcurrentCalls must be greater than zero.");
        }

        if (_options.MaxAutoLockRenewalDurationMinutes < 0)
        {
            throw new InvalidOperationException(
                "Azure Service Bus MaxAutoLockRenewalDurationMinutes cannot be negative.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifecycleLock.WaitAsync();

        try
        {
            await CleanupProcessorAsync();
        }
        finally
        {
            _lifecycleLock.Release();
            _lifecycleLock.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}