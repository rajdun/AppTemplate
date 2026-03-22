using Api.Common;
using Api.Modules.Licence.Requests;
using Application.Common.MediatorPattern;
using Application.Licence.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Licence;

#pragma warning disable CA1515
public sealed partial class LicenceModule
#pragma warning restore CA1515
{
    private static async Task<IResult> RegisterTenant([FromServices] IMediator mediator,
        [FromBody] RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        var command = new RegisterTenantCommand(request.Token);

        var response = await mediator.SendAsync<RegisterTenantCommand, RegisterTenantResult>(command, cancellationToken).ConfigureAwait(false);

        return response.ToHttpResult();
    }

    private static async Task<IResult> ApplyNewToken([FromServices] IMediator mediator,
        [FromBody] ApplyNewTokenRequest request, CancellationToken cancellationToken = default)
    {
        var command = new ApplyNewTokenCommand(request.Token);

        var response = await mediator.SendAsync<ApplyNewTokenCommand, ApplyNewTokenResult>(command, cancellationToken).ConfigureAwait(false);

        return response.ToHttpResult();
    }
}

