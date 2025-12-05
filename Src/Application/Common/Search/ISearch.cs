using Application.Common.Dto;
using Application.Common.Search.Dto;
using Application.Users.Queries;

namespace Application.Common.Search;

public interface ISearch<T> where T : SearchDocumentDto
{
    public Task IndexAsync(IEnumerable<T> documents, CancellationToken cancellationToken = default);

    public Task DeleteAsync(IEnumerable<Guid> documentIds, CancellationToken cancellationToken = default);

    public string IndexName { get; }
}
