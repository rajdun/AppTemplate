namespace Infrastructure.Licence;

internal sealed class LicenceSettings
{
    public const string SectionName = "LicenceSettings";

    public string PublicKeyPath { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}

