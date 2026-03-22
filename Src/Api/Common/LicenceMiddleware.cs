using Application.Common.Interfaces;
using Application.Licence.Services;

namespace Api.Common;

internal sealed class LicenceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILicenceService licenceService)
    {
        ArgumentNullException.ThrowIfNull(licenceService);
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path;

        if (path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase) || path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (path.StartsWithSegments("/api/licence/register", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!await licenceService.IsValidAsync().ConfigureAwait(false))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("Invalid Licence").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
