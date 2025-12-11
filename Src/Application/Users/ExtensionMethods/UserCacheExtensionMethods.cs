using Application.Common.Interfaces;

namespace Application.Users.ExtensionMethods;

internal static class UserCacheExtensionMethods
{
    internal static async Task SaveRefreshTokenAsync(this ICacheService cacheService, Guid userId, string refreshToken, TimeSpan expiration, CancellationToken token = default)
    {
        var cacheKey = $"user_refresh_token_{refreshToken}";
        await cacheService.SetAsync(cacheKey, userId.ToString(), expiration, token).ConfigureAwait(false);
    }

    internal static async Task<Guid?> GetRefreshTokenAsync(this ICacheService cacheService, string refreshToken, CancellationToken token = default)
    {
        var cacheKey = $"user_refresh_token_{refreshToken}";
        var cacheResult = await cacheService.GetAsync<string>(cacheKey, token).ConfigureAwait(false);

        if (Guid.TryParse(cacheResult, out var result))
        {
            return result;
        }

        return null;
    }

    internal static async Task RemoveRefreshTokenAsync(this ICacheService cacheService, string refreshToken, CancellationToken token = default)
    {
        var cacheKey = $"user_refresh_token_{refreshToken}";
        await cacheService.RemoveAsync(cacheKey, token).ConfigureAwait(false);
    }
}
