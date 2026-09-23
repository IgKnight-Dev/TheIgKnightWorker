namespace TheIgKnightWorker.Infrastructure.PostgreQueryConstants;

internal class PostgreDocumentSql
{
    public const string InsertDocument = @"
        INSERT INTO Document
        (
            DocumentId,
            PortfolioId,
            FileName,
            FileUrl,
            FileType,
            UpdatedBy,
            CreatedAt,
            UpdatedAt
        )
        VALUES
        (
            @DocumentId,
            @PortfolioId,
            @FileName,
            @FileUrl,
            @FileType,
            @UpdatedBy,
            @CreatedAt,
            @UpdatedAt
        );";

    public const string GetDocumentById = @"
        SELECT
            DocumentId,
            PortfolioId,
            FileName,
            FileUrl,
            FileType,
            UpdatedBy,
            CreatedAt,
            UpdatedAt
        FROM Document
        WHERE DocumentId = @DocumentId;";

    public const string GetDocumentByPortfolioId = @"
        SELECT
            DocumentId,
            PortfolioId,
            FileName,
            FileUrl,
            FileType,
            UpdatedBy,
            CreatedAt,
            UpdatedAt
        FROM Document
        WHERE PortfolioId = @PortfolioId;";

    public const string DeleteDocument = @"
        DELETE FROM Document
        WHERE DocumentId = @DocumentId;";
}
