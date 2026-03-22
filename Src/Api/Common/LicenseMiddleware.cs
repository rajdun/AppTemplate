using Application.Common.Interfaces;
using Application.License.Services;

namespace Api.Common;

internal sealed class LicenseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILicenseService licenseService)
    {
        ArgumentNullException.ThrowIfNull(licenseService);
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path;
        if (path.StartsWithSegments("/api/license/register", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!await licenseService.IsValidAsync().ConfigureAwait(false))
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("Invalid license").ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
