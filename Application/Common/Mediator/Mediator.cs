using FluentResults;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        
        logger.LogInformation("Handling {RequestType} with {HandlerType}", typeof(TRequest).Name, handler.GetType().Name);

        if (RunValidations<TRequest, TResponse>(request, validators, logger, out Result<TResponse> fail)) 
            return fail;
        
        var result = await handler.Handle(request, cancellationToken);
        logger.LogInformation("Handled {RequestType} with {HandlerType}", typeof(TRequest).Name, handler.GetType().Name);
        
        return result;
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
}