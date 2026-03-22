using Application.Common;
using Application.Licence.Commands;
using Application.Licence.Services;
using Domain.Common.Models;
using ApplicationTests.Common;
using Domain.Aggregates.Licencing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RegisterTenantCommandHandler = Application.Licence.Commands.RegisterTenantCommandHandler;

namespace ApplicationTests.Licence;

public class RegisterTenantCommandHandlerTests
{
    private readonly ILicenceService _licenceService;
    private readonly ILogger<RegisterTenantCommandHandler> _logger;

    public RegisterTenantCommandHandlerTests()
    {
        _licenceService = Substitute.For<ILicenceService>();
        _logger = Substitute.For<ILogger<RegisterTenantCommandHandler>>();
    }

    private static LicenceData ValidLicenceData(string tenantId = "tenant-1") =>
        new(tenantId, "Acme Corp", 100, DateTime.UtcNow.AddDays(30), ["FeatureA"]);

    [Fact]
    public async Task Handle_WithValidToken_AndNewTenant_ShouldCreateLicenceAndReturnTenantId()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenceService.DecodeTokenAsync("valid.token").Returns(ValidLicenceData("new-tenant"));

        var handler = new RegisterTenantCommandHandler(context, _licenceService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-tenant", result.Value.TenantId);
        var stored = context.Licences.Single();
        Assert.Equal("new-tenant", stored.TenantId);
    }

    [Fact]
    public async Task Handle_WhenDecodeTokenThrows_ShouldReturnFailWithError()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        _licenceService.DecodeTokenAsync(Arg.Any<string>()).ThrowsAsync(new InvalidOperationException("bad token"));

        var handler = new RegisterTenantCommandHandler(context, _licenceService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("bad.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Empty(context.Licences);
    }

    [Fact]
    public async Task Handle_WhenTenantAlreadyExists_ShouldReturnFailWithoutAddingDuplicate()
    {
        // Arrange
        await using var context = FakeApplicationDbContext.Create();
        var existing = Domain.Aggregates.Licencing.Licence.Create(
            "existing-tenant", "t", "Corp", DateTime.UtcNow.AddDays(10), 10, []);
        context.Licences.Add(existing);
        await context.SaveChangesAsync(CancellationToken.None);

        _licenceService.DecodeTokenAsync("valid.token").Returns(ValidLicenceData("existing-tenant"));

        var handler = new RegisterTenantCommandHandler(context, _licenceService, _logger);

        // Act
        var result = await handler.Handle(new RegisterTenantCommand("valid.token"), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Single(context.Licences); // no new entry added
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrowArgumentNullException()
    {
        await using var context = FakeApplicationDbContext.Create();
        var handler = new RegisterTenantCommandHandler(context, _licenceService, _logger);

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
