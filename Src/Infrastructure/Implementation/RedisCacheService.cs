using System.Text.Json;
using Application.Common.Interfaces;
using StackExchange.Redis;

namespace Infrastructure.Implementation;

public class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<T>(value.ToString());
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, serializedValue, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _db.KeyDeleteAsync(key);
    }
}