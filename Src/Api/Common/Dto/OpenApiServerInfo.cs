namespace Api.Common.Dto;

internal sealed class OpenApiServerInfo
{
    public Uri Url { get; set; } = new Uri("http://localhost:8080");
    public string Description { get; set; } = string.Empty;
}
