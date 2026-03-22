namespace Domain.Common.Models;

public record LicenseData(string TenantId, string CompanyName, int MaxUsers, DateTime ExpiresAt, IEnumerable<string> ActiveFeatures);
