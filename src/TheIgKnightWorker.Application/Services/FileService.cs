using Microsoft.AspNetCore.Http;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Application.Interfaces;

namespace TheIgKnightWorker.Application.Services;

public class FileService : IFileService
{
    private readonly IFileStorageProvider StorageProvider;
    private readonly IFileValidator? Validator;

    public FileService(IFileStorageProvider storageProvider, IFileValidator? validator = null)
    {
        this.StorageProvider = storageProvider;
        this.Validator = validator;
    }

    public async Task<FileUploadResponseDto> UploadAsync(IFormFile file)
    {
        // Optional validation
        this.Validator?.Validate(file);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";

        using var stream = file.OpenReadStream();

        var url = await this.StorageProvider.UploadAsync(stream, fileName, file.ContentType);

        return new FileUploadResponseDto
        {
            FileName = file.FileName,
            FileUrl = url,
            FileType = file.ContentType
        };
    }

    public async Task DeleteAsync(string fileUrl)
    {
        await this.StorageProvider.DeleteAsync(fileUrl);
    }

    public string GetSecureFileUrl(string fileUrl)
    {
        return this.StorageProvider.GetSecureFileUrl(fileUrl);
    }
}