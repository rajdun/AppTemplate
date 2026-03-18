using Domain.Aggregates.Storage;
using System.Text;

namespace DomainTests.Aggregates.Storage;

public class FileMetadataTests
{
    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldSetAllPropertiesCorrectly()
    {
        // Arrange
        var fileName   = "photo.jpg";
        var mimeType   = "image/jpeg";
        var size       = 1024L;
        var checksum   = "DEADBEEF";
        var provider   = StorageProvider.Local;
        var key        = "images/photo.jpg";

        // Act
        var sut = FileMetadata.Create(fileName, mimeType, size, checksum, provider, key);

        // Assert
        Assert.Equal(fileName,  sut.OriginalFileName);
        Assert.Equal(mimeType,  sut.ContentType);
        Assert.Equal(size,      sut.SizeInBytes);
        Assert.Equal(checksum,  sut.Checksum);
        Assert.Equal(provider,  sut.Provider);
        Assert.Equal(key,       sut.ProviderKey);
    }

    [Fact]
    public void Create_ShouldGenerateNonEmptyGuidId()
    {
        // Act
        var sut = FileMetadata.Create("file.txt", "text/plain", 0, "ABC", StorageProvider.Local, "file.txt");

        // Assert
        Assert.NotEqual(Guid.Empty, sut.Id);
    }

    [Fact]
    public void Create_CalledTwice_ShouldProduceDifferentIds()
    {
        // Act
        var a = FileMetadata.Create("file.txt", "text/plain", 0, "ABC", StorageProvider.Local, "file.txt");
        var b = FileMetadata.Create("file.txt", "text/plain", 0, "ABC", StorageProvider.Local, "file.txt");

        // Assert
        Assert.NotEqual(a.Id, b.Id);
    }

    // ── CreateFromStream ──────────────────────────────────────────────────

    [Fact]
    public void CreateFromStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => FileMetadata.CreateFromStream("file.txt", "text/plain", null!, StorageProvider.Local, "file.txt"));
    }

    [Fact]
    public void CreateFromStream_ShouldComputeChecksumFromStreamContent()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);
        var expectedChecksum = FileMetadata.GetChecksum(content);

        // Act
        var sut = FileMetadata.CreateFromStream("file.txt", "text/plain", stream, StorageProvider.Local, "file.txt");

        // Assert
        Assert.Equal(expectedChecksum, sut.Checksum);
    }

    [Fact]
    public void CreateFromStream_ShouldSetSizeInBytesToStreamLength()
    {
        // Arrange
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var sut = FileMetadata.CreateFromStream("file.txt", "text/plain", stream, StorageProvider.Local, "file.txt");

        // Assert
        // After GetChecksum reads the stream, Position equals the byte count.
        Assert.Equal(content.Length, sut.SizeInBytes);
    }

    [Fact]
    public void CreateFromStream_ShouldSetOtherPropertiesCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream("data"u8.ToArray());

        // Act
        var sut = FileMetadata.CreateFromStream("doc.pdf", "application/pdf", stream, StorageProvider.Local, "docs/doc.pdf");

        // Assert
        Assert.Equal("doc.pdf",          sut.OriginalFileName);
        Assert.Equal("application/pdf",  sut.ContentType);
        Assert.Equal(StorageProvider.Local, sut.Provider);
        Assert.Equal("docs/doc.pdf",     sut.ProviderKey);
    }

    // ── GetChecksum (bytes) ───────────────────────────────────────────────

    [Fact]
    public void GetChecksum_FromBytes_ShouldReturnUpperHexEncodedSHA256()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("test");
        // SHA-256("test") = 9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08
        const string expected = "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08";

        // Act
        var result = FileMetadata.GetChecksum(content);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetChecksum_FromBytes_EmptyArray_ShouldReturnKnownHash()
    {
        // SHA-256("") = e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
        const string expected = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";

        var result = FileMetadata.GetChecksum(Array.Empty<byte>());

        Assert.Equal(expected, result);
    }

    // ── GetChecksum (stream) ──────────────────────────────────────────────

    [Fact]
    public void GetChecksum_FromStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => FileMetadata.GetChecksum((Stream)null!));
    }

    [Fact]
    public void GetChecksum_FromStream_ShouldMatchChecksumFromEquivalentBytes()
    {
        // Arrange
        var content = "Hello, Stream!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var streamChecksum = FileMetadata.GetChecksum(stream);
        var bytesChecksum  = FileMetadata.GetChecksum(content);

        // Assert
        Assert.Equal(bytesChecksum, streamChecksum);
    }
}
