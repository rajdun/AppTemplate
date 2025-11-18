namespace Application.Common.Elasticsearch.Models;

public class ElasticUser : IElasticDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; }  = string.Empty;
    public string Email { get; set; }   = string.Empty;
    
    public static readonly IReadOnlyList<string> SortableFields = ["name", "email"];
}