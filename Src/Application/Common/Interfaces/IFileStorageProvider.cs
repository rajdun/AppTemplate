using Domain.Aggregates.Storage;

namespace Application.Common.Interfaces;

public interface IFileStorageProvider
{
    public StorageProvider Provider { get; }

    public Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
    public Task<Stream> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    public Uri GetPublicUrl(string storageKey);
}

public interface IFileStorageProviderFactory
{
    public IFileStorageProvider GetProvider(StorageProvider provider);
}
