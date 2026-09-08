using Microsoft.AspNetCore.Http;

namespace TheIgKnightWorker.Application.Interfaces;

public interface IFileValidator
{
    void Validate(IFormFile file);
}
