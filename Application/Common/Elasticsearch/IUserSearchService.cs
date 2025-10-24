using Application.Common.Elasticsearch.Dto;
using Application.Common.Elasticsearch.Models;

namespace Application.Common.Elasticsearch;

public interface IUserSearchService
{
    Task<PagedResult<ElasticUser>> SearchUsersAsync(PagedUserRequest request);
}