namespace TheIgKnightWorker.Application.Interfaces;

public interface IResumePdfGenerator
{
    Task<byte[]> GenerateAsync(
        string html,
        CancellationToken cancellationToken = default);
}