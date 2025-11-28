using Domain.Common;
using FluentResults;

namespace Application.Common.Mediator;

public interface IMediator
{
    public Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request,
        CancellationToken cancellationToken = new())
        where TRequest : IRequest<TResponse>;

    public Task<Result> PublishAsync<TRequest>(TRequest request, CancellationToken cancellationToken = new())
        where TRequest : IRequest;
}
