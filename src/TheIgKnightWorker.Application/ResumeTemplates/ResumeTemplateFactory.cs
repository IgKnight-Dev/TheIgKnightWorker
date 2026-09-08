namespace TheIgKnightWorker.Application.ResumeTemplates;

public class ResumeTemplateFactory
{
    private readonly string _basePath;

    public ResumeTemplateFactory(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(AppContext.BaseDirectory, "ResumeTemplates");
    }

    public ResumeTemplateModel GetTemplate(string templateKey)
    {
        var normalizedKey = (templateKey ?? "professional").ToLowerInvariant();

        var templateMap = new Dictionary<string, ResumeTemplateModel>(StringComparer.OrdinalIgnoreCase)
        {
            ["professional"] = new ResumeTemplateModel
            {
                TemplateKey = "professional",
                Title = "Professional Developer",
                Description = "A polished resume focused on experience, projects, and technical depth.",
                HtmlTemplatePath = Path.Combine(_basePath, "professional.html")
            },
            ["experienced"] = new ResumeTemplateModel
            {
                TemplateKey = "experienced",
                Title = "Experienced Developer",
                Description = "A career-growth focused resume with strong emphasis on work history and progress.",
                HtmlTemplatePath = Path.Combine(_basePath, "experienced.html")
            },
            ["fresher"] = new ResumeTemplateModel
            {
                TemplateKey = "fresher",
                Title = "Fresher",
                Description = "A skills-first resume highlighting projects, internships, and education.",
                HtmlTemplatePath = Path.Combine(_basePath, "fresher.html")
            },
            ["student"] = new ResumeTemplateModel
            {
                TemplateKey = "student",
                Title = "Student",
                Description = "An academic-focused resume with projects, education, and achievements.",
                HtmlTemplatePath = Path.Combine(_basePath, "student.html")
            }
        };

        return templateMap.TryGetValue(normalizedKey, out var template)
            ? template
            : templateMap["professional"];
    }
}
