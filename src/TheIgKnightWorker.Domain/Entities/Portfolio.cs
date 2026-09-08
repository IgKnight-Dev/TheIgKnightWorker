namespace TheIgKnightWorker.Domain.Entities;

public class Portfolio
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public string Slug { get; set; } = default!;

    public PortfolioMetaData MetaData { get; set; } = default!;

    public bool IsPublished { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsProfileCompleted { get; set; }

    public Guid CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
