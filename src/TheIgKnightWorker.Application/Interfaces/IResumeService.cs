using TheIgKnightWorker.Application.DTOs;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IResumeService
{
    Task<ResumeGenerationResponseDto> GenerateResumeAsync(
            CreatePortfolioDto portfolioDto,
            ResumeGenerationRequestDto requestDto,
            Guid userId,
            CancellationToken cancellationToken = default);
}
