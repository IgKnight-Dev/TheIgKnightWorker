namespace TheIgKnightWorker.Application.DTOs;

public class FileUploadResponseDto
{
    public string FileName { get; set; } = null!;
    public string FileUrl { get; set; } = null!;
    public string FileType { get; set; } = null!;
}