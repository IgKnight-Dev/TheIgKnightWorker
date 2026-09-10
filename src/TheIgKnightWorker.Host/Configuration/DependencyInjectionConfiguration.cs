using Autofac;
using Autofac.Extensions.DependencyInjection;
using AutoMapper.Contrib.Autofac.DependencyInjection;
using TheIgKnightWorker.Application.DependencyInjection;
using TheIgKnightWorker.Application.MapperProfile;
using TheIgKnightWorker.Infrastructure.DependencyInjection;
using TheIgKnightWorker.Infrastructure.MapperProfile;

namespace TheIgKnightWorker.Configuration;

public static class DependencyInjectionConfiguration
{
    public static IHostBuilder ConfigureWorkerDependencies(
        this IHostBuilder builder)
    {
        builder.UseServiceProviderFactory(
            new AutofacServiceProviderFactory());

        builder.ConfigureServices((context, services) =>
        {
            services.AddWorkerConfiguration(
                context.Configuration);
            services.AddMemoryCache();

            services.AddHostedService<Worker>();
        });

        builder.ConfigureContainer<ContainerBuilder>(
            (context, containerBuilder) =>
            {
                containerBuilder.RegisterModule<InfrastructureModule>();

                containerBuilder.RegisterModule(
                    new ApplicationModule(context.Configuration));

                containerBuilder.RegisterAutoMappers();
            });

        return builder;
    }

    public static void RegisterAutoMappers(
        this ContainerBuilder builder)
    {
        builder.RegisterAutoMapper(
            typeof(InfrastructureMappingProfile).Assembly);

        builder.RegisterAutoMapper(
            typeof(ApplicationMappingProfile).Assembly);
    }
}