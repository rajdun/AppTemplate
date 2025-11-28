using FluentResults;

namespace Domain.Common;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken = new());
}

public interface IRequestHandler<in TRequest> where TRequest : IRequest
{
    public Task<Result> Handle(TRequest request, CancellationToken cancellationToken = new());
}
