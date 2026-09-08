using Microsoft.AspNetCore.Http;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IResumeService
{
    Task<ResumeGenerationResponseDto> GenerateResumeAsync(CreatePortfolioDto portfolio, ResumeGenerationRequestDto requestDto);
    //Task<ResumeGenerationResponseDto> UploadAndAttachResumeAsync(IFormFile file, Guid portfolioId);
}
