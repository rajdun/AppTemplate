using System.Reflection;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Resources;
using Domain.Common;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Application.Common.MediatorPattern;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly Action<ILogger, string, string, Exception?> _logHandling =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(1, nameof(SendAsync)),
            "Handling {RequestType} with {HandlerType}");

    private static readonly Action<ILogger, string, string, Exception?> _logHandled =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(2, nameof(SendAsync)),
            "Handled {RequestType} with {HandlerType}");

    private static readonly Action<ILogger, string, string, Exception?> _logValidationFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(3, "ValidationFailed"),
            "Validation failed for {RequestType}: {Errors}");

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = new())
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();

        _logHandling(logger, typeof(TRequest).Name, handler.GetType().Name, null);

        if (!Authorize<TRequest>(user))
        {
            return new UnauthorizedError(UserTranslations.Unauthorized);
        }

        if (RunValidations<TRequest, TResponse>(request, validators, logger, out var fail))
        {
            return fail;
        }

        var result = await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        _logHandled(logger, typeof(TRequest).Name, handler.GetType().Name, null);

        return result;
    }

    public async Task<Result> PublishAsync<TRequest>(TRequest request, CancellationToken cancellationToken = new())
        where TRequest : IRequest
    {
        var handlersList = _serviceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();
        var stringLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<UserTranslations>>();

        if (handlersList.Count == 0)
        {
            throw new InvalidOperationException("No handlers found");
        }

        if (!Authorize<TRequest>(user))
        {
            return new UnauthorizedError(stringLocalizer["Unauthorized"]);
        }

        if (RunValidations(request, validators, logger, out var fail))
        {
            return fail;
        }

        foreach (var handler in handlersList)
        {
            _logHandling(logger, typeof(TRequest).Name, handler.GetType().Name, null);
            var result = await handler.Handle(request, cancellationToken).ConfigureAwait(false);
            _logHandled(logger, typeof(TRequest).Name, handler.GetType().Name, null);

            if (result.IsFailed)
            {
                return result;
            }
        }

        return Result.Ok();
    }

    private static bool Authorize<T>(IUser user)
    {
        var authorizeAttribute = typeof(T).GetCustomAttribute<AuthorizeAttribute>();

        if (authorizeAttribute == null)
        {
            return true;
        }

        return authorizeAttribute.Authorize(user);
    }

    private static bool RunValidations<TRequest, TResponse>(TRequest request,
        IEnumerable<IValidator<TRequest>> validators, ILogger<Mediator> logger,
        out Result<TResponse> fail) where TRequest : IRequest<TResponse>
    {
        var validatorList = validators.ToList();
        if (validatorList.Count > 0)
        {
            var context = new ValidationContext<TRequest>(request);

            var validatorFailures = validatorList
                .Select(x => x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .ToList();

            if (validatorFailures.Count > 0)
            {
                var errors = string.Join(", ", validatorFailures.Select(f => f.ErrorMessage));
                _logValidationFailed(logger, typeof(TRequest).Name, errors, null);
                fail = Result.Fail<TResponse>(errors);
                return true;
            }
        }

        fail = new Result<TResponse>();
        return false;
    }

    private static bool RunValidations<TRequest>(TRequest request, IEnumerable<IValidator<TRequest>> validators,
        ILogger<Mediator> logger,
        out Result fail) where TRequest : IRequest
    {
        var validatorList = validators.ToList();
        if (validatorList.Count > 0)
        {
            var context = new ValidationContext<TRequest>(request);

            var validatorFailures = validatorList
                .Select(x => x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .ToList();

            if (validatorFailures.Count > 0)
            {
                var errors = string.Join(", ", validatorFailures.Select(f => f.ErrorMessage));
                _logValidationFailed(logger, typeof(TRequest).Name, errors, null);
                fail = Result.Fail(errors);
                return true;
            }
        }

        fail = new Result();
        return false;
    }
}
