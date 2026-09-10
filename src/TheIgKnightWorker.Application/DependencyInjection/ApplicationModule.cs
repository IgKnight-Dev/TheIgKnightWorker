using Autofac;
using Microsoft.Extensions.Configuration;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.Services;

namespace TheIgKnightWorker.Application.DependencyInjection;

public class ApplicationModule : Module
{
    private readonly IConfiguration Configuration;
    public ApplicationModule(IConfiguration configuration)
    {
        this.Configuration = configuration;
    }
    protected override void Load(ContainerBuilder builder)
    {

        builder.RegisterType<FileValidator>()
            .As<IFileValidator>()
            .InstancePerDependency();

        var storageType = this.Configuration["StorageType"];
        Console.WriteLine($"StorageType from config: {storageType}");
        if (storageType == "Blob")
        {
            builder.RegisterType<AzureBlobStorageProvider>()
                   .As<IFileStorageProvider>()
                   .InstancePerLifetimeScope();
        }
        else
        {
            builder.RegisterType<LocalFileStorageProvider>()
                   .As<IFileStorageProvider>()
                   .InstancePerLifetimeScope();
        }

        builder.RegisterType<FileService>()
            .As<IFileService>()
            .InstancePerDependency();

        builder.RegisterType<DocumentService>()
            .As<IDocumentService>()
            .InstancePerDependency();

        builder.RegisterType<ResumeService>()
            .As<IResumeService>()
            .InstancePerDependency();

        builder.RegisterType<ResumeGenerationJobService>()
            .AsSelf()
            .InstancePerDependency();

        builder.RegisterType<ResumeHtmlBuilder>()
                    .As<IResumeHtmlBuilder>()
                    .InstancePerDependency();

        builder.RegisterType<ResumePdfGenerator>()
            .As<IResumePdfGenerator>()
            .SingleInstance();

        builder.RegisterType<ResumeService>()
            .As<IResumeService>()
            .InstancePerDependency();
    }
}