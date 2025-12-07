namespace Domain.Common.Interfaces;

// ReSharper disable once UnusedTypeParameter
#pragma warning disable CA1040
public interface IRequest
#pragma warning restore CA1040
{
}

// ReSharper disable once UnusedTypeParameter
#pragma warning disable CA1040
public interface IRequest<TResponse> : IRequest
#pragma warning restore CA1040
{
}
