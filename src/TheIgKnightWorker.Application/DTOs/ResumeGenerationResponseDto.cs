namespace TheIgKnightWorker.Application.DTOs;

public class ResumeGenerationResponseDto
{
    public Guid DocumentId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string TemplateKey { get; set; } = string.Empty;
    public string ResumeType { get; set; } = "Generated";
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}
