namespace TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

public sealed class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string FullyQualifiedNamespace { get; set; } = string.Empty;

    public string QueueName { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public int MaxConcurrentCalls { get; set; } = 1;

    public int MaxAutoLockRenewalDurationMinutes { get; set; } = 10;

    public string ConnectionString { get; set; } = string.Empty;
}