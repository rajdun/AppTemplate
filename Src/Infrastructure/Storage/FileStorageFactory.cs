using Application.Common.Interfaces;
using Domain.Aggregates.Storage;

namespace Infrastructure.Storage;

public class FileStorageFactory(IEnumerable<IFileStorageProvider> Providers) : IFileStorageProviderFactory
{
    public IFileStorageProvider GetProvider(StorageProvider provider)
    {
        var providerImplementation = Providers.FirstOrDefault(p => p.Provider == provider);
        if (providerImplementation == null)
        {
            throw new NotSupportedException($"Storage provider {provider} is not supported.");
        }

        return providerImplementation;
    }
}
