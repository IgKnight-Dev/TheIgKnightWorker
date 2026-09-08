using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application.DTOs;

public class CreatePortfolioDto
{
    public string Slug { get; set; } = default!;

    public PortfolioMetaData MetaData { get; set; } = default!;

    public List<short> SkillIds { get; set; } = new();
}
