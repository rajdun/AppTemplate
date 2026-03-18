namespace Infrastructure.Storage;

public sealed class LocalFileStorageSettings
{
    public const string SectionName = "LocalFileStorage";

    public string BasePath { get; set; } = Path.Combine(Path.GetTempPath(), "app-storage");

    /// <summary>Base URL used to construct public URLs for stored files (e.g. "https://cdn.example.com/files").</summary>
    public Uri? PublicBaseUrl { get; set; }
}
