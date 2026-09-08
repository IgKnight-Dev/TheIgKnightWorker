namespace TheIgKnightWorker.Application.Interfaces;
public interface IFileStorageProvider
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);

    Task DeleteAsync(string fileUrl);

    string GetSecureFileUrl(string fileUrl);
}