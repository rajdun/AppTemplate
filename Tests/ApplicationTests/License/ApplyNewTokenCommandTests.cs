using Application.License;
using Application.License.Services;
using Domain.Common.Models;
using ApplicationTests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using LicenseEntity = Domain.Aggregates.Licensing.License;

namespace ApplicationTests.License;

public class ApplyNewTokenCommandHandlerTests
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<ApplyNewTokenCommandHandler> _logger;

    public ApplyNewTokenCommandHandlerTests()
    {
        _licenseService = Substitute.For<ILicenseService>();
        _logger = Substitute.For<ILogger<ApplyNewTokenCommandHandler>>();
    }

    private static LicenseData ValidLicenseData(string tenantId = "tenant-1") =>
        new(tenantId, "Acme Corp", 200, DateTime.UtcNow.AddDays(365), ["Feature1", "Feature2"]);

    [Fact]
    public async Task Handle_WithValidTokenAndExistingTenant_ShouldRenewLicenseAndReturnResult()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = LicenseEntity.Create("tenant-1", "old.token", "Old Corp", DateTime.UtcNow.AddDays(10), 50, ["OldFeature"]);
        context.Licenses.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenseService.DecodeTokenAsync("new.token").Returns(ValidLicenseData("tenant-1"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new ApplyNewTokenCommand("new.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Corp", result.Value.CompanyName);
        Assert.Equal(200, result.Value.MaxUsers);
        Assert.Contains("Feature1", result.Value.ActiveFeatures);
        Assert.Contains("Feature2", result.Value.ActiveFeatures);
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldPersistUpdatedLicense()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = LicenseEntity.Create("tenant-1", "old.token", "Old Corp", DateTime.UtcNow.AddDays(10), 50, []);
        context.Licenses.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenseService.DecodeTokenAsync("new.token").Returns(ValidLicenseData("tenant-1"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenseService, _logger);

        // Act
        await handler.Handle(new ApplyNewTokenCommand("new.token"), CancellationToken.None);

        // Assert
        var stored = context.Licenses.Single();
        Assert.Equal("new.token", stored.RawJwtToken);
        Assert.Equal("Acme Corp", stored.CompanyName);
    }

    [Fact]
    public async Task Handle_WhenDecodeTokenThrows_ShouldReturnFail()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenseService.DecodeTokenAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("invalid token"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new ApplyNewTokenCommand("bad.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnFail()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        // context has no licenses
        _licenseService.DecodeTokenAsync("valid.token").Returns(ValidLicenseData("unknown-tenant"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new ApplyNewTokenCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        await using var context = FakeApplicationDbContext.Create();
        var handler = new ApplyNewTokenCommandHandler(context, _licenseService, _logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!, CancellationToken.None));
    }
}

public class ApplyNewTokenCommandValidatorTests
{
    private readonly ApplyNewTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidToken_ShouldPass()
    {
        var result = _validator.Validate(new ApplyNewTokenCommand("some.valid.token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldFail()
    {
        var result = _validator.Validate(new ApplyNewTokenCommand(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ApplyNewTokenCommand.Token));
    }
}
