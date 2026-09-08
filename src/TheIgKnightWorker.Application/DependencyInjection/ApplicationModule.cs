using Autofac;
using AutoMapper;
using Microsoft.Extensions.Logging;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.Services;

namespace TheIgKnightWorker.Application.DependencyInjection;

public class ApplicationModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {

        builder.RegisterType<FileValidator>()
            .As<IFileValidator>()
            .InstancePerDependency();

        builder.RegisterType<LocalFileStorageProvider>()
            .As<IFileStorageProvider>()
            .InstancePerDependency();

        builder.RegisterType<AzureBlobStorageProvider>()
            .Named<IFileStorageProvider>("azure")
            .InstancePerDependency();

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