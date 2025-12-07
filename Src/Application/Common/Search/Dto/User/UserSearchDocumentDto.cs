namespace Application.Common.Search.Dto.User;

public record UserSearchDocumentDto(Guid Id, string Name, string Email) : SearchDocumentDto(Id);
