﻿namespace Application.Common.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public static class CacheKeys
{
    public static string GetRefreshTokenCacheKey(string userId)
    {
        return $"refresh-token:{userId}";
    }
}