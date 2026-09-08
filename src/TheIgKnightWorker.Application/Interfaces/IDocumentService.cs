using Microsoft.AspNetCore.Http;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IDocumentService
{
    Task<(Guid DocumentId, string FileUrl)> UploadAndSaveAsync(IFormFile file, Guid tenantId, Guid portfolioId);
    Task DeleteDocumentAsync(Guid documentId);
    Task<(Guid DocumentId, string FileUrl, string FileName)?> GetDocumentAsync(Guid documentId);
    Task<(Guid DocumentId, string FileUrl, string FileName)?> GetDocumentsByPortfolioIdAsync(Guid portfolioId);
}
