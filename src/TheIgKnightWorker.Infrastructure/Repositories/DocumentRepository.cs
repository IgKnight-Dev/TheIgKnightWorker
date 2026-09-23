using TheIgKnightWorker.Domain.Common.ContextUser;

namespace TheIgKnightWorker.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DapperContext _dapperContext;
    private readonly IRequestContext _requestContext;
    private readonly IMapper _mapper;

    public DocumentRepository(DapperContext dapperContext, IRequestContext requestContext, IMapper mapper)
    {
        _dapperContext = dapperContext;
        _requestContext = requestContext;
        _mapper = mapper;
    }

    public async Task<Guid> AddDocumentAsync(Document document)
    {
        document.DocumentId = document.DocumentId == Guid.Empty ? Guid.NewGuid() : document.DocumentId;
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedBy = _requestContext.User.UserIdentifier;

        var connection = _dapperContext.Connection;
        await connection.ExecuteAsync(PostgreDocumentSql.InsertDocument, document);
        return document.DocumentId;
    }

    public async Task<Document?> GetDocumentByIdAsync(Guid documentId)
    {
        var connection = _dapperContext.Connection;
        var db = await connection.QueryFirstOrDefaultAsync<DbDocument>(PostgreDocumentSql.GetDocumentById, new { DocumentId = documentId });
        return db == null ? null : _mapper.Map<Document>(db);
    }

    public async Task<Document?> GetDocumentsByPortfolioIdAsync(Guid portfolioId)
    {
        var connection = _dapperContext.Connection;
        var db = await connection.QueryFirstOrDefaultAsync<DbDocument>(PostgreDocumentSql.GetDocumentByPortfolioId, new { PortfolioId = portfolioId });
        return db == null ? null : _mapper.Map<Document>(db);
    }

    public async Task DeleteDocumentAsync(Guid documentId)
    {
        var connection = _dapperContext.Connection;
        await connection.ExecuteAsync(PostgreDocumentSql.DeleteDocument, new { DocumentId = documentId });
    }
}
