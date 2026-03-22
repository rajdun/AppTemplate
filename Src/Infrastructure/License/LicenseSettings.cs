namespace Infrastructure.License;

internal sealed class LicenseSettings
{
    public const string SectionName = "LicenseSettings";

    public string PublicKeyPath { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}

