using TheIgKnightWorker.Application.Handlers.Interfaces;
using TheIgKnightWorker.Application.Interfaces;
using TheIgKnightWorker.Application.Messages;

namespace TheIgKnightWorker.Application.Handlers;

public sealed class ResumeGenerationHandler : IResumeGenerationHandler
{
    private readonly IResumeService _resumeService;

    public ResumeGenerationHandler(
        IResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    public async Task HandleAsync(
        ResumeGenerationMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.JobId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Resume generation job ID is required.");
        }

        if (message.UserId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Resume generation user ID is required.");
        }

        ArgumentNullException.ThrowIfNull(message.Portfolio);

        await _resumeService.GenerateResumeAsync(
            message.Portfolio,
            message.Request);
    }
}