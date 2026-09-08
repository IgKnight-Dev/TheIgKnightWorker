using Microsoft.AspNetCore.Http;
using TheIgKnightWorker.Application.DTOs;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IFileService
{
    Task<FileUploadResponseDto> UploadAsync(IFormFile file);

    Task DeleteAsync(string fileUrl);

    string GetSecureFileUrl(string fileUrl);
}
