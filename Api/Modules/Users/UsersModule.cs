using Application.Users.Dto;
using Carter;

namespace Api.Modules.Users;

public partial class UsersModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/users")
            .WithTags("Users");
        
        group.MapPost("login", Login)
            .WithName("Login")
            .Produces<TokenResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithDisplayName("User Login")
            .WithDescription("Autoryzuje użytkownika na podstawie loginu i hasła. Zwraca token JWT oraz refresh token przy poprawnych danych. W przypadku błędnych danych zwraca odpowiedni kod błędu.")
            .WithSummary("Uwierzytelnianie użytkownika. Wymaga loginu i hasła. Token JWT oraz refresh token w odpowiedzi.")
            .WithOpenApi();
        
        group.MapPost("register", Register)
            .WithName("Register")
            .Produces<TokenResult>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithDisplayName("Register a new user")
            .WithDescription("Rejestruje nowego użytkownika na podstawie podanych danych: username, email, password, repeatPassword. Zwraca token JWT oraz refresh token przy poprawnej rejestracji. W przypadku błędnych danych (np. niezgodność haseł, istniejący użytkownik) zwraca kod błędu.")
            .WithSummary("Rejestracja użytkownika. Wymaga username, email, password, repeatPassword. Token JWT oraz refresh token w odpowiedzi.")
            .WithOpenApi();
    }
}