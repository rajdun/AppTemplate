using Application.Common;
using Application.Licence.Services;
using Application.Resources;
using Domain.Common.Interfaces;
using Domain.Common.Models;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Licence.Commands;

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

public partial class ApplyNewTokenCommandHandler(IApplicationDbContext context, ILicenceService licenceService, ILogger<ApplyNewTokenCommandHandler> logger)
    : IRequestHandler<ApplyNewTokenCommand, ApplyNewTokenResult>
{
    public async Task<Result<ApplyNewTokenResult>> Handle(ApplyNewTokenCommand request, CancellationToken cancellationToken = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(request);

        LicenceData licenceData;
        try
        {
            licenceData = await licenceService.DecodeTokenAsync(request.Token).ConfigureAwait(false);
        }
#pragma warning disable CA1031
#pragma warning disable CS0168 // Variable is declared but never used
        catch (Exception ex)
#pragma warning restore CS0168 // Variable is declared but never used
#pragma warning restore CA1031
        {
            LogFailedToDecodeLicenceToken(logger);
            return Result.Fail(LicenceTranslations.InvalidToken);
        }

        var Licence = await context.Licences.FirstOrDefaultAsync(x => x.TenantId == licenceData.TenantId, cancellationToken).ConfigureAwait(false);

        if (Licence == null)
        {
            LogTenantNotFoundTenantid(logger, licenceData.TenantId);
            return Result.Fail(LicenceTranslations.TenantNotFound);
        }

        Licence.Renew(request.Token, licenceData.CompanyName, licenceData.ExpiresAt, licenceData.MaxUsers, licenceData.ActiveFeatures);

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ApplyNewTokenResult(licenceData.CompanyName, licenceData.ExpiresAt, licenceData.MaxUsers, licenceData.ActiveFeatures);
    }

    [LoggerMessage(LogLevel.Error, "Failed to decode Licence token")]
    static partial void LogFailedToDecodeLicenceToken(ILogger<ApplyNewTokenCommandHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Tenant not found: {TenantId}")]
    static partial void LogTenantNotFoundTenantid(ILogger<ApplyNewTokenCommandHandler> logger, string TenantId);
}
