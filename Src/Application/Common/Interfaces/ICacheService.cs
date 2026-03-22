namespace Application.Common.Interfaces;

public interface ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class;

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public static class CacheKeys
{
    public static string GetRefreshTokenCacheKey(string userId)
    {
        return $"api:refresh-token:{userId}";
    }

    public static string GetJtiCacheKey(string jti)
    {
        return $"api:jti:{jti}";
    }

    public static string GetLicenceCacheKey => "api:Licence";
}
