namespace TheIgKnightWorker.Application.DTOs;

public class ResumeGenerationEventData
{
    public Guid? DocumentId { get; set; }
    public string? FileName { get; set; }
    public string? TemplateKey { get; set; }
}
