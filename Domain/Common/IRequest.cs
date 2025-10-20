namespace Domain.Common;

// ReSharper disable once UnusedTypeParameter
public interface IRequest {}
public interface IRequest<TResponse> : IRequest { }