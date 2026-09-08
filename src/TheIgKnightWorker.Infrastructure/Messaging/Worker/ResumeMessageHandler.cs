using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.Messages;
using TheIgKnightWorker.Domain.UnitOfWork;
using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;
using TheIgKnightWorker.Infrastructure.Messaging.Worker;
using static TheIgKnightWorker.Domain.Enums.Enums;

namespace IgKnightContent.ResumeWorker.Handlers;

public class ResumeMessageHandler : IWorkerMessageHandler
{
    private readonly IResumeService _resumeService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResumeMessageHandler> _logger;

    public ResumeMessageHandler(
        IResumeService resumeService,
        IUnitOfWork unitOfWork,
        ILogger<ResumeMessageHandler> logger)
    {
        _resumeService = resumeService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public string MessageType => "Resume";

    public async Task HandleAsync(
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.MessageId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Resume message does not contain a valid EventId.");
        }

        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Resume message does not contain a valid UserId.");
        }

        var eventEntity =
            await _unitOfWork.EventRepository
                .GetEventByJobIdAsync(message.MessageId);

        if (eventEntity == null)
        {
            throw new InvalidOperationException(
                $"Event '{message.MessageId}' was not found.");
        }

        if (eventEntity.Status == EventStatus.Completed)
        {
            return;
        }

        eventEntity.Status =
            EventStatus.InProgress;

        eventEntity.AttemptNumber++;

        eventEntity.UpdatedAt =
            DateTime.UtcNow;

        eventEntity.UpdatedBy =
            message.UserId;

        eventEntity.ErrorCode = null;
        eventEntity.ErrorMessage = null;

        await _unitOfWork.EventRepository
            .UpdateEventAsync(eventEntity);

        try
        {
            if (message.Data.ValueKind == JsonValueKind.Undefined ||
                message.Data.ValueKind == JsonValueKind.Null)
            {
                throw new InvalidOperationException(
                    "Resume message data is empty.");
            }

            var resumeMessage =
                message.Data.Deserialize<ResumeGenerationMessage>(
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (resumeMessage == null)
            {
                throw new InvalidOperationException(
                    "Unable to deserialize resume generation message.");
            }

            if (resumeMessage.JobId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Resume generation message does not contain a valid JobId.");
            }

            if (resumeMessage.Portfolio == null)
            {
                throw new InvalidOperationException(
                    $"Resume generation message {resumeMessage.JobId} " +
                    "does not contain portfolio data.");
            }

            var result =
                await _resumeService.GenerateResumeAsync(
                    resumeMessage.Portfolio,
                    resumeMessage.Request ??
                    new ResumeGenerationRequestDto(),
                    message.UserId,
                    cancellationToken);

            if (result == null)
            {
                throw new InvalidOperationException(
                    $"Resume generation returned a null response. " +
                    $"JobId: {resumeMessage.JobId}");
            }

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Resume generation failed. " +
                    $"JobId: {resumeMessage.JobId}. " +
                    $"Message: {result.Message}");
            }

            eventEntity.Data = JsonSerializer.Serialize(
                        new ResumeGenerationEventData
                        {
                            DocumentId = result.DocumentId,
                            FileName = result.FileName,
                            TemplateKey = result.TemplateKey
                        });

            eventEntity.Status =
                EventStatus.Completed;

            eventEntity.UpdatedAt =
                DateTime.UtcNow;

            eventEntity.UpdatedBy =
                message.UserId;

            eventEntity.ErrorCode = null;
            eventEntity.ErrorMessage = null;

            await _unitOfWork.EventRepository
                .UpdateEventAsync(eventEntity);
        }
        catch (OperationCanceledException)
        {
            await MarkEventFailedAsync(
                eventEntity,
                message.UserId,
                "RESUME_CANCELLED",
                "Resume processing was cancelled.");

            throw;
        }
        catch (Exception ex)
        {
            await MarkEventFailedAsync(
                eventEntity,
                message.UserId,
                "RESUME_GENERATION_FAILED",
                ex.Message);

            throw;
        }
    }

    private async Task MarkEventFailedAsync(
        TheIgKnightWorker.Domain.Entities.Event eventEntity,
        Guid userId,
        string errorCode,
        string errorMessage)
    {
        try
        {
            eventEntity.Status =
                EventStatus.Failed;

            eventEntity.ErrorCode =
                errorCode;

            eventEntity.ErrorMessage =
                errorMessage;

            eventEntity.UpdatedAt =
                DateTime.UtcNow;

            eventEntity.UpdatedBy =
                userId;

            await _unitOfWork.EventRepository
                .UpdateEventAsync(eventEntity);
        }
        catch (Exception updateException)
        {
            Console.WriteLine("Exception:", updateException);
        }
    }
}