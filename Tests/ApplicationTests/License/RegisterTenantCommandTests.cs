using Application.Common;
using Application.License;
using Application.License.Services;
using Domain.Common.Models;
using ApplicationTests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using LicenseEntity = Domain.Aggregates.Licensing.License;

namespace ApplicationTests.License;

public class RegisterTenantCommandHandlerTests
{
    private readonly ILicenseService _licenseService;
    private readonly ILogger<RegisterTenantCommandHandler> _logger;

    public RegisterTenantCommandHandlerTests()
    {
        _licenseService = Substitute.For<ILicenseService>();
        _logger = Substitute.For<ILogger<RegisterTenantCommandHandler>>();
    }

    private static LicenseData ValidLicenseData(string tenantId = "tenant-1") =>
        new(tenantId, "Acme Corp", 100, DateTime.UtcNow.AddDays(30), ["FeatureA"]);

    [Fact]
    public async Task Handle_WithValidToken_AndNewTenant_ShouldCreateLicenseAndReturnTenantId()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenseService.DecodeTokenAsync("valid.token").Returns(ValidLicenseData("new-tenant"));

        var handler = new RegisterTenantCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-tenant", result.Value.TenantId);
        var stored = context.Licenses.Single();
        Assert.Equal("new-tenant", stored.TenantId);
    }

    [Fact]
    public async Task Handle_WhenDecodeTokenThrows_ShouldReturnFailWithError()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenseService.DecodeTokenAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("bad token"));

        var handler = new RegisterTenantCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("bad.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Empty(context.Licenses);
    }

    [Fact]
    public async Task Handle_WhenTenantAlreadyExists_ShouldReturnFailWithoutAddingDuplicate()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = LicenseEntity.Create(
            "existing-tenant", "t", "Corp", DateTime.UtcNow.AddDays(10), 10, []);
        context.Licenses.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenseService.DecodeTokenAsync("valid.token").Returns(ValidLicenseData("existing-tenant"));

        var handler = new RegisterTenantCommandHandler(context, _licenseService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Single(context.Licenses); // no new entry added
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        await using var context = FakeApplicationDbContext.Create();
        var handler = new RegisterTenantCommandHandler(context, _licenseService, _logger);

        await Assert.ThrowsAsync<ArgumentNullException>(() => handler.Handle(null!, CancellationToken.None));
    }
}

public class RegisterTenantCommandValidatorTests
{
    private readonly RegisterTenantCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidToken_ShouldPass()
    {
        var result = _validator.Validate(new RegisterTenantCommand("some.valid.token"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyToken_ShouldFail()
    {
        var result = _validator.Validate(new RegisterTenantCommand(string.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterTenantCommand.Token));
    }
}
