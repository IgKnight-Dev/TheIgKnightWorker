using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application.Messages;

public sealed class ResumeGenerationMessage
{
    public Guid JobId { get; init; }

    public Guid UserId { get; init; }

    public CreatePortfolioDto Portfolio { get; init; } = default!;

    public ResumeGenerationRequestDto Request { get; init; } = default!;
}
