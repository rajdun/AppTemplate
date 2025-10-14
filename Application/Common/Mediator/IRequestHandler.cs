using FluentResults;

namespace Application.Common.Mediator;

public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<Result<TResponse>> Handle(TRequest request);
}