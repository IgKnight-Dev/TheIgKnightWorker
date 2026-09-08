namespace TheIgKnightWorker.Domain.RepositoryInterfaces;

public interface IDocumentRepository
{
    Task<Guid> AddDocumentAsync(Document document);
    Task<Document?> GetDocumentByIdAsync(Guid documentId);
    Task<Document?> GetDocumentsByPortfolioIdAsync(Guid portfolioId);
    Task DeleteDocumentAsync(Guid documentId);
}