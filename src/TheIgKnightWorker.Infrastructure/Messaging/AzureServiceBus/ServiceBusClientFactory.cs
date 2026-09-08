using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

public sealed class ServiceBusClientFactory
{
    private readonly AzureServiceBusOptions _options;

    public ServiceBusClientFactory(
        IOptions<AzureServiceBusOptions> options)
    {
        _options = options.Value;
    }

    public ServiceBusClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(
            _options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Azure Service Bus connection string is not configured.");
        }

        return new ServiceBusClient(
            _options.ConnectionString);
    }
}