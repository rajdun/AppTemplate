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

public partial class RegisterTenantCommandHandler(IApplicationDbContext context, ILicenceService licenceService, ILogger<RegisterTenantCommandHandler> logger)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    public async Task<Result<RegisterTenantResult>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken = new CancellationToken())
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
            return Result.Fail<RegisterTenantResult>(LicenceTranslations.InvalidToken);
        }

        if (await context.Licences.AnyAsync(x => x.TenantId == licenceData.TenantId, cancellationToken)
                .ConfigureAwait(false))
        {
            LogTenantAlreadyTaken(logger);
            return Result.Fail<RegisterTenantResult>(LicenceTranslations.TenantAlreadyTaken);
        }

        var Licence = Domain.Aggregates.Licencing.Licence.Create(
            licenceData.TenantId,
            request.Token,
            licenceData.CompanyName,
            licenceData.ExpiresAt,
            licenceData.MaxUsers,
            licenceData.ActiveFeatures);

        context.Licences.Add(Licence);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new RegisterTenantResult(licenceData.TenantId);
    }

    [LoggerMessage(LogLevel.Error, "Failed to decode Licence token")]
    static partial void LogFailedToDecodeLicenceToken(ILogger<RegisterTenantCommandHandler> logger);

    [LoggerMessage(LogLevel.Warning, "Tenant already taken")]
    static partial void LogTenantAlreadyTaken(ILogger<RegisterTenantCommandHandler> logger);
}
