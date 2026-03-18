using Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace InfrastructureTests.Storage;

/// <summary>
/// Integration-style tests for <see cref="LocalFileProvider"/> that operate on
/// a real temporary directory and clean up after themselves.
/// </summary>
public sealed class LocalFileProviderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalFileProvider _sut;

    public LocalFileProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LocalFileProviderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var settings = new LocalFileStorageSettings
        {
            BasePath       = _tempDir,
            PublicBaseUrl  = new Uri("http://localhost:8080/files/")
        };
        _sut = new LocalFileProvider(Options.Create(settings));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // ── Provider property ─────────────────────────────────────────────────

    [Fact]
    public void Provider_ShouldReturnLocal()
    {
        Assert.Equal(Domain.Aggregates.Storage.StorageProvider.Local, _sut.Provider);
    }

    // ── SaveAsync ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_WithNullOrWhitespaceStorageKey_ShouldThrowArgumentException(string? key)
    {
        using var stream = new MemoryStream("data"u8.ToArray());

        await Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await _sut.SaveAsync(key!, stream));
    }

    [Fact]
    public async Task SaveAsync_WithNullContent_ShouldThrowArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _sut.SaveAsync("file.txt", null!));
    }

    [Fact]
    public async Task SaveAsync_ShouldWriteFileToCorrectPath()
    {
        // Arrange
        var content = "Hello, Local Storage!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        await _sut.SaveAsync("file.txt", stream);

        // Assert
        var expectedPath = Path.Combine(_tempDir, "file.txt");
        Assert.True(File.Exists(expectedPath));
        Assert.Equal(content, await File.ReadAllBytesAsync(expectedPath));
    }

    [Fact]
    public async Task SaveAsync_ShouldCreateIntermediateDirectories()
    {
        // Arrange
        using var stream = new MemoryStream("nested"u8.ToArray());

        // Act
        await _sut.SaveAsync("a/b/c/file.txt", stream);

        // Assert
        Assert.True(File.Exists(Path.Combine(_tempDir, "a", "b", "c", "file.txt")));
    }

    [Fact]
    public async Task SaveAsync_CalledTwice_ShouldOverwriteFile()
    {
        // Arrange
        var first  = "first"u8.ToArray();
        var second = "second"u8.ToArray();

        await _sut.SaveAsync("overwrite.txt", new MemoryStream(first));
        await _sut.SaveAsync("overwrite.txt", new MemoryStream(second));

        // Assert
        var path = Path.Combine(_tempDir, "overwrite.txt");
        Assert.Equal(second, await File.ReadAllBytesAsync(path));
    }

    // ── GetAsync ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_WithNullOrWhitespaceStorageKey_ShouldThrowArgumentException(string? key)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await _sut.GetAsync(key!));
    }

    [Fact]
    public async Task GetAsync_WhenFileDoesNotExist_ShouldThrowFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await _sut.GetAsync("nonexistent.txt"));
    }

    [Fact]
    public async Task GetAsync_WhenFileExists_ShouldReturnCorrectContent()
    {
        // Arrange
        var content = "stream content"u8.ToArray();
        await _sut.SaveAsync("readable.txt", new MemoryStream(content));

        // Act
        await using var result = await _sut.GetAsync("readable.txt");
        using var buffer = new MemoryStream();
        await result.CopyToAsync(buffer);

        // Assert
        Assert.Equal(content, buffer.ToArray());
    }

    [Fact]
    public async Task GetAsync_WhenFileExists_ShouldReturnReadableStream()
    {
        // Arrange
        await _sut.SaveAsync("check.txt", new MemoryStream("ok"u8.ToArray()));

        // Act
        await using var stream = await _sut.GetAsync("check.txt");

        // Assert
        Assert.True(stream.CanRead);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAsync_WithNullOrWhitespaceStorageKey_ShouldThrowArgumentException(string? key)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await _sut.DeleteAsync(key!));
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_ShouldRemoveFile()
    {
        // Arrange
        await _sut.SaveAsync("delete-me.txt", new MemoryStream("bye"u8.ToArray()));

        // Act
        await _sut.DeleteAsync("delete-me.txt");

        // Assert
        Assert.False(File.Exists(Path.Combine(_tempDir, "delete-me.txt")));
    }

    [Fact]
    public async Task DeleteAsync_WhenFileDoesNotExist_ShouldNotThrow()
    {
        // Should complete without throwing.
        await _sut.DeleteAsync("ghost.txt");
    }

    // ── GetPublicUrl ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetPublicUrl_WithNullOrWhitespaceStorageKey_ShouldThrowArgumentException(string? key)
    {
        Assert.ThrowsAny<ArgumentException>(() => _sut.GetPublicUrl(key!));
    }

    [Fact]
    public void GetPublicUrl_WhenPublicBaseUrlNotConfigured_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var settings = new LocalFileStorageSettings
        {
            BasePath      = _tempDir,
            PublicBaseUrl = null
        };
        var provider = new LocalFileProvider(Options.Create(settings));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => provider.GetPublicUrl("file.txt"));
    }

    [Fact]
    public void GetPublicUrl_ShouldReturnCorrectUrl()
    {
        // Act
        var url = _sut.GetPublicUrl("images/photo.jpg");

        // Assert
        Assert.Equal("http://localhost:8080/files/images/photo.jpg", url.AbsoluteUri);
    }

    [Fact]
    public void GetPublicUrl_ShouldEncodeSpecialCharactersInSegments()
    {
        // "my file.jpg" → "my%20file.jpg"
        var url = _sut.GetPublicUrl("uploads/my file.jpg");

        Assert.Contains("my%20file.jpg", url.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public void GetPublicUrl_ShouldPreserveSlashesAsDirectorySeparators()
    {
        // Slashes between segments must NOT be encoded.
        var url = _sut.GetPublicUrl("a/b/c.txt");

        Assert.Equal("http://localhost:8080/files/a/b/c.txt", url.AbsoluteUri);
    }

    // ── Path traversal protection ─────────────────────────────────────────

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("../../etc/passwd")]
    public async Task SaveAsync_WithPathTraversalKey_ShouldThrowUnauthorizedAccessException(string key)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.SaveAsync(key, new MemoryStream("x"u8.ToArray())));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("../../etc/passwd")]
    public async Task GetAsync_WithPathTraversalKey_ShouldThrowUnauthorizedAccessException(string key)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.GetAsync(key));
    }

    [Theory]
    [InlineData("../secret.txt")]
    [InlineData("../../etc/passwd")]
    public async Task DeleteAsync_WithPathTraversalKey_ShouldThrowUnauthorizedAccessException(string key)
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await _sut.DeleteAsync(key));
    }
}
