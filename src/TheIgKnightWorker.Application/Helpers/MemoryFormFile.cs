using Microsoft.AspNetCore.Http;

namespace TheIgKnightWorker.Application.Helpers;

public class MemoryFormFile : IFormFile
{
    private readonly Stream _stream;

    public MemoryFormFile(Stream stream, string name, string fileName, string contentType)
    {
        _stream = stream ?? throw new System.ArgumentNullException(nameof(stream));
        Name = name;
        FileName = fileName;
        ContentType = contentType;
        Headers = new HeaderDictionary();
        ContentDisposition = $"form-data; name=\"{name}\"; filename=\"{fileName}\"";
    }

    public string ContentType { get; }

    public string ContentDisposition { get; }

    public IHeaderDictionary Headers { get; set; }

    public long Length => _stream.Length;

    public string Name { get; }

    public string FileName { get; }

    public Stream OpenReadStream()
    {
        if (_stream.CanSeek)
            _stream.Position = 0;
        return _stream;
    }

    public void CopyTo(Stream target)
    {
        OpenReadStream().CopyTo(target);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        return OpenReadStream().CopyToAsync(target, cancellationToken);
    }
}
