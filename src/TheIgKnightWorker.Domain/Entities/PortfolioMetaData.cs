using static TheIgKnightWorker.Domain.Enums.Enums;

namespace TheIgKnightWorker.Domain.Entities;

public class PortfolioMetaData
{
    public BasicInfo BasicInfo { get; set; } = new();
    public string Summary { get; set; } = default!;
    public string ShortDescription { get; set; } = default!;

    public List<PortfolioSkill> SkillList { get; set; } = new();

    public List<string> CustomSkills { get; set; }

    public List<Experience> Experience { get; set; } = new();
    public List<Project> Projects { get; set; } = new();

    public List<Education> Education { get; set; } = new();

    public List<PortfolioAchievement> Achievements { get; set; } = new();

    // Optional resume reference (stores the Document record id and an optional cached URL)
    public Guid? ResumeDocumentId { get; set; }
    public string? ResumeUrl { get; set; }
    public string? ResumeFileName { get; set; }
}


public class Skill
{
    public short Id { get; set; }

    public string Name { get; set; } = default!;
    public SkillCategory Category { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
