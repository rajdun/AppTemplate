namespace Api.Modules.Users.Requests;

#pragma warning disable CA1515
public sealed record RegisterRequest(string Username, string Password, string RepeatPassword, string Email);
#pragma warning restore CA1515
