using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using TheIgKnightWorker.Application.Interfaces;

namespace TheIgKnightWorker.Application.Services;

public class AzureBlobStorageProvider : IFileStorageProvider
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageProvider(IConfiguration configuration)
    {
        var connectionString = configuration["BlobConnectionString"];
        var containerName = "uploads";

        if (string.IsNullOrEmpty(connectionString))
            throw new Exception("BlobConnectionString is missing");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadAsync(
        Stream stream,
        string fileName,
        string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        await blobClient.UploadAsync(
            stream,
            new BlobHttpHeaders
            {
                ContentType = contentType
            });

        return GenerateSecureFileUrl(blobClient);
    }

    public string GetSecureFileUrl(string fileUrl)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
        var blobClient = _containerClient.GetBlobClient(fileName);

        return GenerateSecureFileUrl(blobClient);
    }

    public async Task DeleteAsync(string fileUrl)
    {
        var fileName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
        var blobClient = _containerClient.GetBlobClient(fileName);

        await blobClient.DeleteIfExistsAsync();
    }

    private string GenerateSecureFileUrl(BlobClient blobClient)
    {
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        return blobClient.GenerateSasUri(sasBuilder).ToString();
    }
}
