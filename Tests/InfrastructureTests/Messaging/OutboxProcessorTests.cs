using Application.Common;
using Application.Common.Interfaces;
using Hangfire;
using Infrastructure.Data;
using Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InfrastructureTests.Messaging;

public class OutboxProcessorTests
{
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IDateTimeProvider _dateTimeProvider;

    public OutboxProcessorTests()
    {
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _logger = Substitute.For<ILogger<OutboxProcessor>>();
        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
    }

    [Fact]
    public void OutboxProcessor_ShouldBeCreatedWithDependencies()
    {
        // Arrange
        var dbContext = Substitute.For<ApplicationDbContext>();

        // Act
        var processor = new OutboxProcessor(_logger, dbContext, _backgroundJobClient, _dateTimeProvider);

        // Assert
        Assert.NotNull(processor);
    }
}

