using Infrastructure.Implementation;
using NSubstitute;
using StackExchange.Redis;
using System.Text.Json;

namespace InfrastructureTests.Implementation;

public class RedisCacheServiceTests
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly RedisCacheService _sut;

    public RedisCacheServiceTests()
    {
        _redis = Substitute.For<IConnectionMultiplexer>();
        _database = Substitute.For<IDatabase>();
        _redis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(_database);
        _sut = new RedisCacheService(_redis);
    }

    [Fact]
    public async Task GetAsync_WhenKeyExists_ShouldReturnDeserializedObject()
    {
        // Arrange
        var key = "test-key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var serialized = JsonSerializer.Serialize(testObject);
        
        _database.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(new RedisValue(serialized));

        // Act
        var result = await _sut.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testObject.Id, result.Id);
        Assert.Equal(testObject.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var key = "non-existent-key";
        _database.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(RedisValue.Null);

        // Act
        var result = await _sut.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenValueIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var key = "empty-key";
        _database.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(RedisValue.EmptyString);

        // Act
        var result = await _sut.GetAsync<TestCacheObject>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ShouldSerializeAndStoreObject()
    {
        // Arrange
        var key = "test-key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };
        var expiration = TimeSpan.FromMinutes(10);

        // Act
        await _sut.SetAsync(key, testObject, expiration);

        // Assert
        await _database.Received(1).StringSetAsync(
            key,
            Arg.Is<RedisValue>(v => v.ToString().Contains("Test")),
            expiration,
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WithoutExpiration_ShouldStoreWithoutTTL()
    {
        // Arrange
        var key = "test-key";
        var testObject = new TestCacheObject { Id = 1, Name = "Test" };

        // Act
        await _sut.SetAsync(key, testObject);

        // Assert
        await _database.Received(1).StringSetAsync(
            key,
            Arg.Any<RedisValue>(),
            null,
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteKey()
    {
        // Arrange
        var key = "test-key";

        // Act
        await _sut.RemoveAsync(key);

        // Assert
        await _database.Received(1).KeyDeleteAsync(key, Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task SetAsync_WithComplexObject_ShouldSerializeCorrectly()
    {
        // Arrange
        var key = "complex-key";
        var complexObject = new ComplexCacheObject
        {
            Id = 1,
            Name = "Complex",
            Tags = new List<string> { "tag1", "tag2" },
            Metadata = new Dictionary<string, string> { { "key1", "value1" } }
        };

        // Act
        await _sut.SetAsync(key, complexObject);

        // Assert
        await _database.Received(1).StringSetAsync(
            key,
            Arg.Is<RedisValue>(v => 
                v.ToString().Contains("Complex") && 
                v.ToString().Contains("tag1")),
            null,
            Arg.Any<bool>(),
            Arg.Any<When>(),
            Arg.Any<CommandFlags>());
    }

    [Fact]
    public async Task GetAsync_WithComplexObject_ShouldDeserializeCorrectly()
    {
        // Arrange
        var key = "complex-key";
        var complexObject = new ComplexCacheObject
        {
            Id = 1,
            Name = "Complex",
            Tags = new List<string> { "tag1", "tag2" },
            Metadata = new Dictionary<string, string> { { "key1", "value1" } }
        };
        var serialized = JsonSerializer.Serialize(complexObject);
        
        _database.StringGetAsync(key, Arg.Any<CommandFlags>())
            .Returns(new RedisValue(serialized));

        // Act
        var result = await _sut.GetAsync<ComplexCacheObject>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(complexObject.Id, result.Id);
        Assert.Equal(complexObject.Name, result.Name);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains("tag1", result.Tags);
        Assert.Single(result.Metadata);
    }

    private class TestCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ComplexCacheObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}

