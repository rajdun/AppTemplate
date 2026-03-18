using Domain.Common.Models;

namespace Domain.Aggregates.Storage;

public class FileMetadata : AggregateRoot<Guid>
{
    public string OriginalFileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeInBytes { get; private set; }
    // SHA256 hash of the file content, used for integrity checks and deduplication
    public string Checksum { get; private set; }

    public StorageProvider Provider { get; private set; }
    public string ProviderKey { get; private set; }

    private FileMetadata(string originalFileName, string contentType, long sizeInBytes, string checksum, StorageProvider provider, string providerKey)
    {
        Id = Guid.CreateVersion7();
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        Checksum = checksum;
        Provider = provider;
        ProviderKey = providerKey;
    }

    public static FileMetadata Create(string originalFileName, string contentType, long sizeInBytes, string checksum,
        StorageProvider provider, string providerKey)
    {
        return new FileMetadata(originalFileName, contentType, sizeInBytes, checksum, provider, providerKey);
    }

    public static FileMetadata CreateFromStream(string originalFileName, string contentType, Stream fileStream, StorageProvider provider, string providerKey)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var checksum = GetChecksum(fileStream);
        var sizeInBytes = fileStream.Position;

        return new FileMetadata(originalFileName, contentType, sizeInBytes, checksum, provider, providerKey);
    }

    public static string GetChecksum(byte[] fileContent)
    {
        var hashBytes = System.Security.Cryptography.SHA256.HashData(fileContent);
        return Convert.ToHexString(hashBytes);
    }

    public static string GetChecksum(Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var hashBytes = System.Security.Cryptography.SHA256.HashData(fileStream);
        return Convert.ToHexString(hashBytes);
    }
}

public enum StorageProvider
{
    Local
}
