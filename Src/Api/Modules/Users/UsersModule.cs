using Application.Common.Dto;
using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;
using Application.Users.Dto;
using Carter;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Users;

public partial class UsersModule : ICarterModule
{
    private const string JsonContentType = "application/json";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users")
            .WithTags("Users");

        group.MapPost("login", Login)
            .WithName("Login")
            .Produces<TokenResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithDisplayName("User Login")
            .WithDescription(
                "Autoryzuje użytkownika na podstawie loginu i hasła. Zwraca token JWT oraz refresh token przy poprawnych danych. W przypadku błędnych danych zwraca odpowiedni kod błędu.")
            .WithSummary(
                "Uwierzytelnianie użytkownika. Wymaga loginu i hasła. Token JWT oraz refresh token w odpowiedzi.");

        group.MapPost("register", Register)
            .WithName("Register")
            .Produces<TokenResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithDisplayName("Register a new user")
            .WithDescription(
                "Rejestruje nowego użytkownika na podstawie podanych danych: username, email, password, repeatPassword. Zwraca token JWT oraz refresh token przy poprawnej rejestracji. W przypadku błędnych danych (np. niezgodność haseł, istniejący użytkownik) zwraca kod błędu.")
            .WithSummary(
                "Rejestracja użytkownika. Wymaga username, email, password, repeatPassword. Token JWT oraz refresh token w odpowiedzi.");

        group.MapPost("refresh-token", RefreshToken)
            .WithName("RefreshToken")
            .RequireAuthorization()
            .Produces<TokenResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithDisplayName("Refresh User Token")
            .WithDescription(
                "Odświeża token JWT na podstawie przesłanego refresh tokena. Zwraca nowy token JWT oraz refresh token przy poprawnych danych. W przypadku błędnych danych (np. nieważny refresh token) zwraca odpowiedni kod błędu.")
            .WithSummary(
                "Odświeżanie tokena użytkownika. Wymaga refresh tokena. Nowy token JWT oraz refresh token w odpowiedzi.");

        group.MapPost("search", SearchUsers)
            .WithName("SearchUsers")
            .RequireAuthorization()
            .Produces<PagedResult<ElasticUser>>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithDisplayName("Search Users")
            .WithDescription("Wyszukuje użytkowników na podstawie podanych kryteriów. Zwraca paginowaną listę użytkowników spełniających kryteria wyszukiwania.")
            .WithSummary("Wyszukiwanie użytkowników z paginacją na podstawie podanych kryteriów.");

        group.MapDelete("{userId:guid}/deactivate", DeactivateUser)
            .WithName("DeactivateUser")
            .RequireAuthorization()
            .Produces<DeactivateUserResult>(StatusCodes.Status200OK, JsonContentType)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDisplayName("Deactivate User")
            .WithDescription("Dezaktywuje użytkownika o podanym identyfikatorze. Zwraca wynik operacji dezaktywacji. W przypadku nieistniejącego użytkownika lub błędnych danych zwraca odpowiedni kod błędu.")
            .WithSummary("Dezaktywacja użytkownika na podstawie jego identyfikatora.");
    }
}
