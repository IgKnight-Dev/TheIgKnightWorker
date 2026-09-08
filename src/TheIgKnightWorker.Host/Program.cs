using TheIgKnightWorker.Configuration;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureWorkerDependencies();

var host = builder.Build();

host.Run();