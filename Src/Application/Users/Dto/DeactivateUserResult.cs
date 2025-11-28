namespace Application.Users.Dto;

public record DeactivateUserResult(Guid UserId, string? Name, string? Email);
