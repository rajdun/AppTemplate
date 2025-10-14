using FluentResults;

namespace Application.Common.Mediator;

public interface IMediator
{
    Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request) 
        where TRequest : IRequest<TResponse>;
}