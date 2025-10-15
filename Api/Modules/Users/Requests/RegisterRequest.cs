namespace Api.Modules.Users.Requests;

public record RegisterRequest(string Username, string Password, string RepeatPassword, string? Email);