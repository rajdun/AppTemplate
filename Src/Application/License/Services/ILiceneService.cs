using Domain.Common.Models;

namespace Application.License.Services;

public interface ILicenseService
{
    public Task<LicenseData> DecodeTokenAsync(string token);

    public Task<bool> IsValidAsync();
}
