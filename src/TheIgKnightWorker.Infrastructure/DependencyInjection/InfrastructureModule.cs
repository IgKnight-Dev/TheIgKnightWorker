using Autofac;
using IgKnightContent.ResumeWorker.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using TheIgKnightWorker.Domain.Common.ContextUser;
using TheIgKnightWorker.Domain.UnitOfWork;
using TheIgKnightWorker.Infrastructure.DataBase;
using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;
using TheIgKnightWorker.Infrastructure.Messaging.Worker;
using TheIgKnightWorker.Infrastructure.Repositories;

namespace TheIgKnightWorker.Infrastructure.DependencyInjection;

public sealed class InfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Azure Service Bus / Worker messaging registrations
        builder.RegisterType<ServiceBusClientFactory>()
            .SingleInstance();

        builder.RegisterType<ServiceBusMessageSerializer>()
            .SingleInstance();

        builder.RegisterType<QueueMessageReceiver>()
            .SingleInstance();

        builder.RegisterType<WorkerExecutor>()
            .SingleInstance();

        builder.RegisterType<DebugWorkerMessageHandler>()
            .As<IWorkerMessageHandler>()
            .SingleInstance();

        builder.RegisterType<ResumeMessageHandler>()
            .As<IWorkerMessageHandler>()
            .SingleInstance();

        // Infrastructure data / repositories / unit of work
        // DapperContext holds DB connection/transaction - create per-lifetime-scope so transactions work correctly.
        builder.RegisterType<DapperContext>()
            .InstancePerLifetimeScope();

        // UnitOfWork depends on DapperContext and ILifetimeScope
        builder.RegisterType<UnitOfWork>()
            .As<IUnitOfWork>()
            .InstancePerLifetimeScope();

        // Concrete repositories
        builder.RegisterType<DocumentRepository>()
            .As<Domain.RepositoryInterfaces.IDocumentRepository>()
            .InstancePerDependency();

        builder.RegisterType<SkillRepository>()
            .As<Domain.RepositoryInterfaces.ISkillRepository>()
            .InstancePerDependency();

        builder.RegisterType<ContextUserRepository>()
            .As<Domain.RepositoryInterfaces.IContextUserRepository>()
            .InstancePerDependency();

        // Request context / HTTP-related helpers used across app
        // Register minimal IHttpContextAccessor and IMemoryCache so RequestContext can be resolved when not registered elsewhere.
        builder.RegisterType<HttpContextAccessor>()
            .As<IHttpContextAccessor>()
            .SingleInstance();

        // RequestContext depends on IHttpContextAccessor, IMemoryCache and IUnitOfWork
        builder.RegisterType<RequestContext>()
            .As<IRequestContext>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EventRepository>().As<IEventRepository>().InstancePerLifetimeScope();
    }
}