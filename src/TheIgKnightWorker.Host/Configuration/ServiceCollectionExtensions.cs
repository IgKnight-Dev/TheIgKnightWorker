using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;

namespace TheIgKnightWorker.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<AzureServiceBusOptions>()
            .Bind(configuration.GetSection(
                AzureServiceBusOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.FullyQualifiedNamespace),
                "Azure Service Bus fully qualified namespace is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.QueueName),
                "Azure Service Bus queue name is required.")
            .ValidateOnStart();

        return services;
    }

    public static IHostBuilder UpdateConfigurationKeys(this IHostBuilder builder)
    {
        var keyVaultUriString = "https://ignight-dev-kv.vault.azure.net/";

        var keyVaultUri = new Uri(keyVaultUriString);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddAzureKeyVault(
                keyVaultUri,
                new DefaultAzureCredential());
        });

        return builder;
    }
}