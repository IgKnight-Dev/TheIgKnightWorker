using Microsoft.Extensions.Logging;
using System.Text.Json;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.Messages;
using TheIgKnightWorker.Infrastructure.Messaging.AzureServiceBus;
using TheIgKnightWorker.Infrastructure.Messaging.Worker;

namespace IgKnightContent.ResumeWorker.Handlers;

public class ResumeMessageHandler : IWorkerMessageHandler
{
    private readonly IResumeService _resumeService;
    private readonly ILogger<ResumeMessageHandler> _logger;

    public ResumeMessageHandler(
        IResumeService resumeService,
        ILogger<ResumeMessageHandler> logger)
    {
        _resumeService = resumeService;
        _logger = logger;
    }

    public string MessageType => "Resume";

    public async Task HandleAsync(
        WorkerMessage message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Resume message received. MessageId: {MessageId}",
            message.MessageId);

        try
        {
            if (string.IsNullOrWhiteSpace(message.Data.ToString()))
            {
                throw new InvalidOperationException(
                    "Resume message data is empty.");
            }

            var resumeMessage =
                JsonSerializer.Deserialize<ResumeGenerationMessage>(
                    message.Data,
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
                    new ResumeGenerationRequestDto());


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

        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Resume processing cancelled. MessageId: {MessageId}",
                message.MessageId);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Resume processing failed. " +
                "MessageId: {MessageId}",
                message.MessageId);

            throw;
        }
    }
}