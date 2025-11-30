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
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();

        _logHandling(logger, typeof(TRequest).Name, handler.GetType().Name, null);

        // 1. Authorization
        if (!Authorize<TRequest>(user))
        {
            return new UnauthorizedError(UserTranslations.Unauthorized);
        }

        // 2. Async Validation
        var validationError = await RunValidationAndGetErrorAsync(request, logger).ConfigureAwait(false);
        if (validationError != null)
        {
            return Result.Fail<TResponse>(validationError);
        }

        // 3. Handling
        var result = await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        _logHandled(logger, typeof(TRequest).Name, handler.GetType().Name, null);

        return result;
    }

    public async Task<Result> PublishAsync<TRequest>(TRequest request, CancellationToken cancellationToken = new())
        where TRequest : IRequest
    {
        var handlersList = _serviceProvider.GetServices<IRequestHandler<TRequest>>().ToList();
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();
        var stringLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<UserTranslations>>();

        if (handlersList.Count == 0)
        {
            throw new InvalidOperationException($"No handlers found for {typeof(TRequest).Name}");
        }

        // 1. Authorization
        if (!Authorize<TRequest>(user))
        {
            return new UnauthorizedError(stringLocalizer["Unauthorized"]);
        }

        // 2. Async Validation
        var validationError = await RunValidationAndGetErrorAsync(request, logger).ConfigureAwait(false);
        if (validationError != null)
        {
            return Result.Fail(validationError);
        }

        // 3. Handling (Iterate all handlers)
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

    /// <summary>
    /// Uruchamia walidatory asynchronicznie. Zwraca string z błędami lub null, jeśli wszystko jest OK.
    /// </summary>
    private async Task<string?> RunValidationAndGetErrorAsync<TRequest>(TRequest request, ILogger logger)
    {
        // Pobieramy walidatory tutaj, żeby nie przekazywać ich jako parametr
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>().ToList();

        if (validators.Count == 0)
        {
            return null; // Brak walidatorów = sukces
        }

        var context = new ValidationContext<TRequest>(request);

        // Uruchamiamy wszystkie walidacje równolegle (Task.WhenAll)
        var validationTasks = validators.Select(v => v.ValidateAsync(context));
        var validationResults = await Task.WhenAll(validationTasks).ConfigureAwait(false);

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count > 0)
        {
            var errorsString = string.Join(", ", failures.Select(f => f.ErrorMessage));
            _logValidationFailed(logger, typeof(TRequest).Name, errorsString, null);
            return errorsString;
        }

        return null;
    }
}
