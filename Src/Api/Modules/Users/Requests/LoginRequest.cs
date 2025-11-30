namespace Api.Modules.Users.Requests;

#pragma warning disable CA1515
public sealed record LoginRequest(string Login, string Password);
#pragma warning restore CA1515
