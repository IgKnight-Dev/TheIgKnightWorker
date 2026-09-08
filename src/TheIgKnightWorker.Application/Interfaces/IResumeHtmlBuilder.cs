using TheIgKnightWorker.Application.ResumeTemplates;
using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IResumeHtmlBuilder
{
    string Build(
        ResumeTemplateModel template,
        Portfolio portfolio,
        List<Skill> skills);
}