using Application.Common.Interfaces;
using Application.Resources;
using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Reflection;

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
    /// Runs all validators for the given request and logs any validation failures.
    /// If any validation fails, it sets the 'fail' output parameter to a failed Result
    /// containing the validation error messages and returns true. If all validations
    /// pass, it returns false and 'fail' is set to a successful Result.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="validators"></param>
    /// <param name="logger"></param>
    /// <param name="fail"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
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
}