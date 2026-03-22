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

public record RegisterTenantCommand(
    string Token) : IRequest<RegisterTenantResult>;

public record RegisterTenantResult(string TenantId);

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty();
    }
}

public partial class RegisterTenantCommandHandler(IApplicationDbContext context, ILicenseService licenseService, ILogger<RegisterTenantCommandHandler> logger)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public async Task<Result<RegisterTenantResult>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken = new CancellationToken())
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
            return Result.Fail<RegisterTenantResult>(LicenseTranslations.InvalidToken);
        }

        if (await context.Licenses.AnyAsync(x => x.TenantId == licenseData.TenantId, cancellationToken)
                .ConfigureAwait(false))
        {
            LogTenantAlreadyTaken(logger);
            return Result.Fail<RegisterTenantResult>(LicenseTranslations.TenantAlreadyTaken);
        }

        var license = Domain.Aggregates.Licensing.License.Create(
            licenseData.TenantId,
            request.Token,
            licenseData.CompanyName,
            licenseData.ExpiresAt,
            licenseData.MaxUsers,
            licenseData.ActiveFeatures);

        context.Licenses.Add(license);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RegisterTenantResult(licenseData.TenantId);
    }

    [LoggerMessage(LogLevel.Error, "Failed to decode license token")]
    static partial void LogFailedToDecodeLicenseToken(ILogger<RegisterTenantCommandHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Tenant already taken")]
    static partial void LogTenantAlreadyTaken(ILogger<RegisterTenantCommandHandler> logger);
}
