using Application.Common.Interfaces;
using Domain.Aggregates.Storage;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class LocalFileProvider(IOptions<LocalFileStorageSettings> options) : IFileStorageProvider
{
    private const int BufferSize = 81_920;

    private readonly string _basePath =
        Path.GetFullPath(options.Value.BasePath).TrimEnd(Path.DirectorySeparatorChar)
        + Path.DirectorySeparatorChar;

    private readonly Uri? _publicBaseUrl = options.Value.PublicBaseUrl;

    public StorageProvider Provider => StorageProvider.Local;

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentNullException.ThrowIfNull(content);

        var filePath = GetSafePath(storageKey);

        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: BufferSize, useAsync: true);
        await using (fileStream.ConfigureAwait(false))
        {
            await content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var filePath = GetSafePath(storageKey);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found for storage key '{storageKey}'.", filePath);
        }

        Stream stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: BufferSize, useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var filePath = GetSafePath(storageKey);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Uri GetPublicUrl(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        if (_publicBaseUrl is null)
        {
            throw new InvalidOperationException(
                $"'{LocalFileStorageSettings.SectionName}:{nameof(LocalFileStorageSettings.PublicBaseUrl)}' is not configured.");
        }

        // Encode each path segment individually to preserve slashes used as directory separators.
        var encodedKey = string.Join('/', storageKey.Split('/').Select(Uri.EscapeDataString));
        return new Uri(_publicBaseUrl, encodedKey);
    }

    private string GetSafePath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, storageKey));

        if (!fullPath.StartsWith(_basePath, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Access to path outside the storage directory is not allowed.");
        }

        return fullPath;
    }
}
