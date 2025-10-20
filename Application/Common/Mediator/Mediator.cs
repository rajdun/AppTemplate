using Application.Common.Interfaces;
using Application.Resources;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Domain.Common;

namespace Application.Common.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = new())
        where TRequest : IRequest<TResponse>
    {
        var handler = _serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>();
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();
        var stringLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<UserTranslations>>();
        
        logger.LogInformation("Handling {RequestType} with {HandlerType}", typeof(TRequest).Name, handler.GetType().Name);
        if (!Authorize<TRequest>(user))
            return new Errors.UnauthorizedError(stringLocalizer["Unauthorized"]);
        
        if (RunValidations<TRequest, TResponse>(request, validators, logger, out Result<TResponse> fail)) 
            return fail;
        
        var result = await handler.Handle(request, cancellationToken);
        logger.LogInformation("Handled {RequestType} with {HandlerType}", typeof(TRequest).Name, handler.GetType().Name);
        
        return result;
    }

    public async Task<Result> PublishAsync<TRequest>(TRequest request, CancellationToken cancellationToken = new CancellationToken()) where TRequest : IRequest
    {
        var handlers = _serviceProvider.GetServices<IRequestHandler<TRequest>>();
        var validators = _serviceProvider.GetServices<IValidator<TRequest>>();
        var logger = _serviceProvider.GetRequiredService<ILogger<Mediator>>();
        var user = _serviceProvider.GetRequiredService<IUser>();
        var stringLocalizer = _serviceProvider.GetRequiredService<IStringLocalizer<UserTranslations>>();
        
        if (!handlers.Any())
        {
            // Change to string localizer
            return Result.Fail($"No handlers found for {typeof(TRequest).Name}");
        }
        
        if (!Authorize<TRequest>(user))
            return new Errors.UnauthorizedError(stringLocalizer["Unauthorized"]);
        
        if (RunValidations<TRequest>(request, validators, logger, out Result fail)) 
            return fail;

        foreach (var handler in handlers)
        {
            logger.LogInformation("Handling {RequestType} with {HandlerType}", typeof(TRequest).Name, handler.GetType().Name);
            var result = await handler.Handle(request, cancellationToken);
            logger.LogInformation("Handled {RequestType} with {HandlerType}", typeof(TRequest).Name,
                handler.GetType().Name);

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
    
    private static bool RunValidations<TRequest, TResponse>(TRequest request, IEnumerable<IValidator<TRequest>> validators, ILogger<Mediator> logger,
        out Result<TResponse> fail) where TRequest : IRequest<TResponse>
    {
        var validatorList = validators.ToList();
        if (validatorList.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validatorFailures = validatorList
                .Select(x => x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .ToList();

            if (validatorFailures.Any())
            {
                var errors = string.Join(", ", validatorFailures.Select(f => f.ErrorMessage));
                logger.LogWarning("Validation failed for {RequestType}: {Errors}", typeof(TRequest).Name, errors);
                fail = Result.Fail<TResponse>(errors);
                return true;
            }
        }
        
        fail = new Result<TResponse>();
        return false;
    }
    
    private static bool RunValidations<TRequest>(TRequest request, IEnumerable<IValidator<TRequest>> validators, ILogger<Mediator> logger,
        out Result fail) where TRequest : IRequest
    {
        var validatorList = validators.ToList();
        if (validatorList.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validatorFailures = validatorList
                .Select(x => x.Validate(context))
                .SelectMany(x => x.Errors)
                .Where(x => x != null)
                .ToList();

            if (validatorFailures.Any())
            {
                var errors = string.Join(", ", validatorFailures.Select(f => f.ErrorMessage));
                logger.LogWarning("Validation failed for {RequestType}: {Errors}", typeof(TRequest).Name, errors);
                fail = Result.Fail(errors);
                return true;
            }
        }
        
        fail = new Result();
        return false;
    }
}