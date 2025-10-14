using Application.Common.Mediator;
using FluentResults;

namespace Application.Cqrs.Example.Query;

public record ExampleQuery(string Name) : IRequest<string>;

internal class ExampleQueryHandler : IRequestHandler<ExampleQuery, string>
{
    public async Task<Result<string>> Handle(ExampleQuery request, CancellationToken cancellationToken = new())
    {
        await Task.Delay(100, cancellationToken);
        return Result.Ok($"Hello, {request.Name}!");
    }
}