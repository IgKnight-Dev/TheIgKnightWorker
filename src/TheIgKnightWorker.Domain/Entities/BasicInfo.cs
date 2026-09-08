using static TheIgKnightWorker.Domain.Enums.Enums;

namespace TheIgKnightWorker.Domain.Entities;

public class BasicInfo
{
    public string FullName { get; set; } = default!;
    public string Headline { get; set; } = default!;
    public string Location { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;

    public List<PortfolioLink> Links { get; set; } = new();
}

public class PortfolioSkill
{
    public Guid Id { get; set; }

    public Guid PortfolioId { get; set; }
    public SkillCategory Category { get; set; }

    public short SkillId { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class Experience
{
    public string Company { get; set; } = default!;
    public string Role { get; set; } = default!;
    public string Location { get; set; } = default!;

    public string StartDate { get; set; } = default!;
    public string EndDate { get; set; } = default!;

    public string Description { get; set; } = default!;
}

public class Project
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;

    public string? LiveUrl { get; set; }
    public string? RepositoryUrl { get; set; }

    public List<string> Highlights { get; set; } = new();
}

public class Education
{
    public string Degree { get; set; } = default!;
    public string Institution { get; set; } = default!;

    public int StartYear { get; set; }
    public int? EndYear { get; set; }
}

public class PortfolioAchievement
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Organization { get; set; }

    public string? Date { get; set; }

    public string? Link { get; set; }
}

public class PortfolioLink
{
    public PortfolioLinkType Type { get; set; }
    public string Url { get; set; } = default!;
}
