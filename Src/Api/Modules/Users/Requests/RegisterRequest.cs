namespace Api.Modules.Users.Requests;

internal sealed record RegisterRequest(string Username, string Password, string RepeatPassword, string? Email);
