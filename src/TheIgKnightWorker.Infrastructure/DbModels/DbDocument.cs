using System.ComponentModel.DataAnnotations.Schema;

namespace TheIgKnightWorker.Infrastructure.DbModels.DbDocument;
[Table("Document")]
public class DbDocument
{
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PortfolioId { get; set; }
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;

    public Guid? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}