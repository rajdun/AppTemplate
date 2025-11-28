using Application.Common.Dto;
using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;

namespace Application.Common.Elasticsearch;

public interface IUserSearchService
{
    public Task<PagedResult<ElasticUser>> SearchUsersAsync(PagedUserRequest request, CancellationToken cancellationToken = new());
}
