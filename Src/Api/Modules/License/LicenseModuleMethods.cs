using Api.Common;
using Api.Modules.License.Requests;
using Application.Common.MediatorPattern;
using Application.License;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.License;

#pragma warning disable CA1515
public sealed partial class LicenseModule
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

