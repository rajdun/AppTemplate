using Application.Common.Interfaces;
using Application.Licence.Services;
using Domain.Aggregates.Licencing.DomainNotification;
using Domain.Common.Interfaces;
using FluentResults;

namespace Application.Licence.NotificationHandlers;

public class LicenceRegeneratedNotificationHandler(ICacheService cache, ILicenceService licenceService) : IRequestHandler<LicenceRegenerated>
{
    public async Task<Result> Handle(LicenceRegenerated request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        var LicenceData = await licenceService.DecodeTokenAsync(request.RawJwtToken).ConfigureAwait(false);

        var key = CacheKeys.GetLicenceCacheKey;
        await cache.SetAsync(key, LicenceData, null, cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }
}
