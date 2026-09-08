using Microsoft.AspNetCore.Http;
using TheIgKnightWorker.Application.Interfaces;

namespace TheIgKnightWorker.Application.Services;

public class FileValidator : IFileValidator
{
    private readonly string[] AllowedTypes = { "application/pdf", "image/jpeg", "image/png" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public void Validate(IFormFile file)
    {
        if (file.Length == 0)
            throw new Exception("File is empty");

        if (file.Length > MaxFileSize)
            throw new Exception("File size exceeds limit (5MB)");

        if (!AllowedTypes.Contains(file.ContentType))
            throw new Exception("Invalid file type");
    }
}
