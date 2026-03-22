namespace Domain.Common.Models;

public record LicenceData(string TenantId, string CompanyName, int MaxUsers, DateTime ExpiresAt, IEnumerable<string> ActiveFeatures);
