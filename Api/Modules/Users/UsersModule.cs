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
            .Produces<TokenResult>(StatusCodes.Status200OK)
            .WithDisplayName("User Login")
            .WithDescription("Authenticates a user with the provided username and password.")
            .WithSummary("User Login Endpoint")
            .WithOpenApi();
        
        group.MapPost("register", Register)
            .WithName("Register")
            .Produces<TokenResult>(StatusCodes.Status200OK)
            .WithDisplayName("Register a new user")
            .WithDescription("Registers a new user with the provided username, email, and password.")
            .WithSummary("User Registration Endpoint")
            .WithOpenApi();
    }
}