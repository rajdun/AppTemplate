using Application.Common.Interfaces;
using Domain.Aggregates.Storage;
using Infrastructure.Storage;
using NSubstitute;

namespace InfrastructureTests.Storage;

public class FileStorageFactoryTests
{
    private static IFileStorageProvider MakeProvider(StorageProvider providerType)
    {
        var provider = Substitute.For<IFileStorageProvider>();
        provider.Provider.Returns(providerType);
        return provider;
    }

    [Fact]
    public void GetProvider_WhenMatchingProviderRegistered_ShouldReturnIt()
    {
        // Arrange
        var localProvider = MakeProvider(StorageProvider.Local);
        var sut = new FileStorageFactory([localProvider]);

        // Act
        var result = sut.GetProvider(StorageProvider.Local);

        // Assert
        Assert.Same(localProvider, result);
    }

    [Fact]
    public void GetProvider_WhenNoMatchingProviderRegistered_ShouldThrowNotSupportedException()
    {
        // Arrange — empty provider list
        var sut = new FileStorageFactory([]);

        // Act & Assert
        Assert.Throws<NotSupportedException>(
            () => sut.GetProvider(StorageProvider.Local));
    }

    [Fact]
    public void GetProvider_ExceptionMessage_ShouldContainProviderName()
    {
        // Arrange
        var sut = new FileStorageFactory([]);

        // Act
        var ex = Assert.Throws<NotSupportedException>(
            () => sut.GetProvider(StorageProvider.Local));

        // Assert
        Assert.Contains(StorageProvider.Local.ToString(), ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetProvider_WithMultipleProviders_ShouldReturnCorrectOne()
    {
        // Arrange — if additional providers are added in future, the factory
        // must still resolve the right one.
        var localProvider  = MakeProvider(StorageProvider.Local);
        // Add a second duplicate to verify FirstOrDefault picks the right type.
        var anotherLocal   = MakeProvider(StorageProvider.Local);
        var sut = new FileStorageFactory([localProvider, anotherLocal]);

        // Act
        var result = sut.GetProvider(StorageProvider.Local);

        // Assert — first match wins
        Assert.Same(localProvider, result);
    }
}
