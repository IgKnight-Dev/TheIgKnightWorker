using TheIgKnightWorker.Application.Interfaces;

namespace TheIgKnightWorker.Application.Services;

public class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string BasePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    private readonly string BaseUrl = "https://localhost:7153/uploads";

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        if (!Directory.Exists(BasePath))
            Directory.CreateDirectory(BasePath);

        var filePath = Path.Combine(BasePath, fileName);

        using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return $"{BaseUrl}/{fileName}";
    }

    public string GetSecureFileUrl(string fileUrl)
    {
        return fileUrl;
    }

    public Task DeleteAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(fileUrl);
        var filePath = Path.Combine(BasePath, fileName);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}