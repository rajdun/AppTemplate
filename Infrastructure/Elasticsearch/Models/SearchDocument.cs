using System;

namespace Infrastructure.Elasticsearch
{
    public class SearchDocument
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
namespace Infrastructure.Elasticsearch
{
    public class ElasticSearchOptions
    {
        public string Uri { get; set; } = "http://localhost:9200";
        public bool EnablePocBackgroundWorker { get; set; } = true;
        public int PocIntervalSeconds { get; set; } = 30;
    }
}

