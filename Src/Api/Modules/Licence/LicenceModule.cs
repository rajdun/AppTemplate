using Application.Licence.Commands;
using Carter;

namespace Api.Modules.Licence;

#pragma warning disable CA1515
public sealed partial class LicenceModule : ICarterModule
#pragma warning restore CA1515
{
    private const string JsonContentType = "application/json";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/licence")
            .WithTags("Licence");

        group.MapPut("register", RegisterTenant)
            .WithName("RegisterTenant")
            .Produces<RegisterTenantResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithDisplayName("Register Tenant")
            .WithDescription(
                "Rejestruje nowego tenanta na podstawie przesłanego tokenu licencyjnego. Dekoduje token, weryfikuje jego poprawność i zapisuje dane licencji w systemie. Zwraca identyfikator tenanta. W przypadku błędnych danych lub już istniejącego tenanta zwraca odpowiedni kod błędu.")
            .WithSummary(
                "Rejestracja tenanta. Wymaga tokenu licencyjnego. Zwraca identyfikator tenanta.");

        group.MapPatch("apply-token", ApplyNewToken)
            .WithName("ApplyNewToken")
            .RequireAuthorization()
            .Produces<ApplyNewTokenResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDisplayName("Apply New Licence Token")
            .WithDescription(
                "Aktualizuje licencję istniejącego tenanta na podstawie nowego tokenu licencyjnego. Dekoduje token, weryfikuje jego poprawność i odnawia dane licencji (data ważności, liczba użytkowników, aktywne funkcje). W przypadku nieistniejącego tenanta lub błędnych danych zwraca odpowiedni kod błędu.")
            .WithSummary(
                "Aktualizacja licencji tenanta. Wymaga autoryzacji i nowego tokenu licencyjnego. Zwraca zaktualizowane dane licencji.");
    }
}


