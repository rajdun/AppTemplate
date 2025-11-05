using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Mediator;
using Application.Resources;
using Domain.Common;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ApplicationTests.Common.Mediator;

public class MediatorTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Application.Common.Mediator.Mediator> _logger;
    private readonly IUser _user;
    private readonly IStringLocalizer<UserTranslations> _localizer;

    public MediatorTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = Substitute.For<ILogger<Application.Common.Mediator.Mediator>>();
        _user = Substitute.For<IUser>();
        _localizer = Substitute.For<IStringLocalizer<UserTranslations>>();

        var serviceScope = Substitute.For<IServiceScope>();
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        
        serviceScope.ServiceProvider.Returns(_serviceProvider);
        serviceScopeFactory.CreateScope().Returns(serviceScope);

        _serviceProvider.GetService(typeof(ILogger<Application.Common.Mediator.Mediator>)).Returns(_logger);
        _serviceProvider.GetService(typeof(IUser)).Returns(_user);
        _serviceProvider.GetService(typeof(IStringLocalizer<UserTranslations>)).Returns(_localizer);
    }

    [Fact]
    public async Task SendAsync_WithValidRequest_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new TestRequest();
        var handler = Substitute.For<IRequestHandler<TestRequest, string>>();
        handler.Handle(request, Arg.Any<CancellationToken>()).Returns(Result.Ok("Success"));

        _serviceProvider.GetService(typeof(IRequestHandler<TestRequest, string>)).Returns(handler);
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<TestRequest>>))
            .Returns(Enumerable.Empty<IValidator<TestRequest>>());
        _user.IsAuthenticated.Returns(true);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.SendAsync<TestRequest, string>(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Success", result.Value);
        await handler.Received(1).Handle(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithUnauthenticatedUser_ShouldReturnUnauthorizedError()
    {
        // Arrange
        var request = new AuthorizedRequest();
        var handler = Substitute.For<IRequestHandler<AuthorizedRequest, string>>();

        _serviceProvider.GetService(typeof(IRequestHandler<AuthorizedRequest, string>)).Returns(handler);
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<AuthorizedRequest>>))
            .Returns(Enumerable.Empty<IValidator<AuthorizedRequest>>());
        _user.IsAuthenticated.Returns(false);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.SendAsync<AuthorizedRequest, string>(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.IsType<UnauthorizedError>(result.Errors[0]);
        await handler.DidNotReceive().Handle(Arg.Any<AuthorizedRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WithValidationErrors_ShouldReturnFailedResult()
    {
        // Arrange
        var request = new TestRequest();
        var handler = Substitute.For<IRequestHandler<TestRequest, string>>();
        var validator = Substitute.For<IValidator<TestRequest>>();
        
        var validationFailure = new ValidationFailure("Property", "Error message");
        var validationResult = new ValidationResult(new[] { validationFailure });
        validator.Validate(Arg.Any<ValidationContext<TestRequest>>()).Returns(validationResult);

        _serviceProvider.GetService(typeof(IRequestHandler<TestRequest, string>)).Returns(handler);
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<TestRequest>>))
            .Returns(new[] { validator });
        _user.IsAuthenticated.Returns(true);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.SendAsync<TestRequest, string>(request);

        // Assert
        Assert.True(result.IsFailed);
        Assert.Contains("Error message", result.Errors[0].Message);
        await handler.DidNotReceive().Handle(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WithNoHandlers_ShouldReturnFailedResult()
    {
        // Arrange
        var request = new TestNotification();

        _serviceProvider.GetService(typeof(IEnumerable<IRequestHandler<TestNotification>>))
            .Returns(Enumerable.Empty<IRequestHandler<TestNotification>>());
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<TestNotification>>))
            .Returns(Enumerable.Empty<IValidator<TestNotification>>());
        _user.IsAuthenticated.Returns(true);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.PublishAsync(request);

        // Assert
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task PublishAsync_WithMultipleHandlers_ShouldExecuteAll()
    {
        // Arrange
        var request = new TestNotification();
        var handler1 = Substitute.For<IRequestHandler<TestNotification>>();
        var handler2 = Substitute.For<IRequestHandler<TestNotification>>();
        
        handler1.Handle(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());
        handler2.Handle(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        _serviceProvider.GetService(typeof(IEnumerable<IRequestHandler<TestNotification>>))
            .Returns(new[] { handler1, handler2 });
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<TestNotification>>))
            .Returns(Enumerable.Empty<IValidator<TestNotification>>());
        _user.IsAuthenticated.Returns(true);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.PublishAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        await handler1.Received(1).Handle(request, Arg.Any<CancellationToken>());
        await handler2.Received(1).Handle(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_WhenHandlerFails_ShouldStopExecutionAndReturnError()
    {
        // Arrange
        var request = new TestNotification();
        var handler1 = Substitute.For<IRequestHandler<TestNotification>>();
        var handler2 = Substitute.For<IRequestHandler<TestNotification>>();
        
        handler1.Handle(request, Arg.Any<CancellationToken>()).Returns(Result.Fail("Handler failed"));
        handler2.Handle(request, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        _serviceProvider.GetService(typeof(IEnumerable<IRequestHandler<TestNotification>>))
            .Returns(new[] { handler1, handler2 });
        _serviceProvider.GetService(typeof(IEnumerable<IValidator<TestNotification>>))
            .Returns(Enumerable.Empty<IValidator<TestNotification>>());
        _user.IsAuthenticated.Returns(true);

        var mediator = new Application.Common.Mediator.Mediator(_serviceProvider);

        // Act
        var result = await mediator.PublishAsync(request);

        // Assert
        Assert.True(result.IsFailed);
        await handler1.Received(1).Handle(request, Arg.Any<CancellationToken>());
        await handler2.DidNotReceive().Handle(request, Arg.Any<CancellationToken>());
    }

    public class TestRequest : IRequest<string> { }

    [Authorize(AuthorizePolicy.Admin)]
    public class AuthorizedRequest : IRequest<string> { }

    public class TestNotification : IRequest { }
}

