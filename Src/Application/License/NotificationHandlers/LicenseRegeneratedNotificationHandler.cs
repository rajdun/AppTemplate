using Application.Common;
using Application.Common.Interfaces;
using Application.License.Services;
using Domain.Aggregates.Licensing.DomainNotification;
using Domain.Common.Interfaces;
using FluentResults;
using Microsoft.Extensions.Configuration;

namespace Application.License.NotificationHandlers;

public class LicenseRegeneratedNotificationHandler(ICacheService cache, ILicenseService licenseService) : IRequestHandler<LicenseRegenerated>
{
    public async Task<Result> Handle(LicenseRegenerated request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        var licenseData = await licenseService.DecodeTokenAsync(request.RawJwtToken).ConfigureAwait(false);

        var key = CacheKeys.GetLicenseCacheKey;
        await cache.SetAsync(key, licenseData, null, cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }
}
