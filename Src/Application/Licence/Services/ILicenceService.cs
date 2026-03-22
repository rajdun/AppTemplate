using Domain.Common.Models;

namespace Application.Licence.Services;

public interface ILicenceService
{
    public Task<LicenceData> DecodeTokenAsync(string token);

    public Task<bool> IsValidAsync();
}
