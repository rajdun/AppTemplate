using Application.Common;
using Application.License.Services;
using Application.Resources;
using Domain.Common.Interfaces;
using Domain.Common.Models;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.License;

public record ApplyNewTokenCommand(string Token) : IRequest<ApplyNewTokenResult>;
public record ApplyNewTokenResult(string CompanyName, DateTime ExpiresAt, int MaxUsers, IEnumerable<string> ActiveFeatures);

public class ApplyNewTokenCommandValidator : AbstractValidator<ApplyNewTokenCommand>
{
    public ApplyNewTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}

public partial class ApplyNewTokenCommandHandler(IApplicationDbContext context, ILicenseService licenseService, ILogger<ApplyNewTokenCommandHandler> logger)
    : IRequestHandler<ApplyNewTokenCommand, ApplyNewTokenResult>
{
    public async Task<Result<ApplyNewTokenResult>> Handle(ApplyNewTokenCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        LicenseData licenseData;
        try
        {
            licenseData = await licenseService.DecodeTokenAsync(request.Token).ConfigureAwait(false);
        }
#pragma warning disable CA1031
#pragma warning disable CS0168 // Variable is declared but never used
        catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning restore CA1031
        {
            LogFailedToDecodeLicenseToken(logger);
            return Result.Fail(LicenseTranslations.InvalidToken);
        }

        var license = await context.Licenses.FirstOrDefaultAsync(x => x.TenantId == licenseData.TenantId, cancellationToken).ConfigureAwait(false);

        if (license == null)
        {
            LogTenantNotFoundTenantid(logger, licenseData.TenantId);
            return Result.Fail(LicenseTranslations.TenantNotFound);
        }

        license.Renew(request.Token, licenseData.CompanyName, licenseData.ExpiresAt, licenseData.MaxUsers, licenseData.ActiveFeatures);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApplyNewTokenResult(licenseData.CompanyName, licenseData.ExpiresAt, licenseData.MaxUsers, licenseData.ActiveFeatures);
    }

    [LoggerMessage(LogLevel.Error, "Failed to decode license token")]
    static partial void LogFailedToDecodeLicenseToken(ILogger<ApplyNewTokenCommandHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Tenant not found: {TenantId}")]
    static partial void LogTenantNotFoundTenantid(ILogger<ApplyNewTokenCommandHandler> logger, string TenantId);
}
