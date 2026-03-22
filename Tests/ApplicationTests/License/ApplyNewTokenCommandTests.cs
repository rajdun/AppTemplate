using Application.Licence.Commands;
using Application.Licence.Services;
using Domain.Common.Models;
using ApplicationTests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApplyNewTokenCommandHandler = Application.Licence.Commands.ApplyNewTokenCommandHandler;

namespace ApplicationTests.Licence;

public class ApplyNewTokenCommandHandlerTests
{
    private readonly ILicenceService _licenceService;
    private readonly ILogger<ApplyNewTokenCommandHandler> _logger;

    public ApplyNewTokenCommandHandlerTests()
    {
        _licenceService = Substitute.For<ILicenceService>();
        _logger = Substitute.For<ILogger<ApplyNewTokenCommandHandler>>();
    }

    private static LicenceData ValidLicenceData(string tenantId = "tenant-1") =>
        new(tenantId, "Acme Corp", 200, DateTime.UtcNow.AddDays(365), ["Feature1", "Feature2"]);

    [Fact]
    public async Task Handle_WithValidTokenAndExistingTenant_ShouldRenewLicenceAndReturnResult()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = Domain.Aggregates.Licencing.Licence.Create("tenant-1", "old.token", "Old Corp", DateTime.UtcNow.AddDays(10), 50, ["OldFeature"]);
        context.Licences.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenceService.DecodeTokenAsync("new.token").Returns(ValidLicenceData("tenant-1"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenceService, _logger);

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
    public async Task Handle_WithValidToken_ShouldPersistUpdatedLicence()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = Domain.Aggregates.Licencing.Licence.Create("tenant-1", "old.token", "Old Corp", DateTime.UtcNow.AddDays(10), 50, []);
        context.Licences.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenceService.DecodeTokenAsync("new.token").Returns(ValidLicenceData("tenant-1"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenceService, _logger);

        // Act
        await handler.Handle(new ApplyNewTokenCommand("new.token"), CancellationToken.None);

        // Assert
        var stored = context.Licences.Single();
        Assert.Equal("new.token", stored.RawJwtToken);
        Assert.Equal("Acme Corp", stored.CompanyName);
    }

    [Fact]
    public async Task Handle_WhenDecodeTokenThrows_ShouldReturnFail()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenceService.DecodeTokenAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("invalid token"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenceService, _logger);

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
        // context has no Licences
        _licenceService.DecodeTokenAsync("valid.token").Returns(ValidLicenceData("unknown-tenant"));

        var handler = new ApplyNewTokenCommandHandler(context, _licenceService, _logger);

        // Act
        var result = await handler.Handle(new ApplyNewTokenCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        await using var context = FakeApplicationDbContext.Create();
        var handler = new ApplyNewTokenCommandHandler(context, _licenceService, _logger);

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
