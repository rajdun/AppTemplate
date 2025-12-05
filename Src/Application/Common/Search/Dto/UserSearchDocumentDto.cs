namespace Application.Common.Search.Dto;

public record UserSearchDocumentDto(Guid Id, string Name, string Email) : SearchDocumentDto(Id);
