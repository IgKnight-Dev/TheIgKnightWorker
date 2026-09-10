using TheIgKnightWorker.Configuration;

var builder = Host.CreateDefaultBuilder(args);

// In non-development environments, load secrets from Azure Key Vault
// into the configuration.
builder.UpdateConfigurationKeys();

builder.ConfigureWorkerDependencies();

var host = builder.Build();

host.Run();