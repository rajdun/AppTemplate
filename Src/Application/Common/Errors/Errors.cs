using FluentResults;

namespace Application.Common.Errors;

/// <summary>
///     Błąd wskazujący, że żądany zasób nie został znaleziony.
///     Tłumaczony na status HTTP 404 Not Found.
/// </summary>
public class NotFoundError : Error
{
    public NotFoundError(string message) : base(message)
    {
    }
}

/// <summary>
///     Błąd wskazujący na konflikt, np. próba utworzenia zasobu, który już istnieje.
///     Tłumaczony na status HTTP 409 Conflict.
/// </summary>
public class ConflictError : Error
{
    public ConflictError(string message) : base(message)
    {
    }
}

/// <summary>
///     Błąd walidacji, który powinien być używany do jawnego sygnalizowania problemów z danymi wejściowymi.
///     Tłumaczony na status HTTP 400 Bad Request z ValidationProblemDetails.
/// </summary>
public class ValidationError : Error
{
    public ValidationError(string message) : base(message)
    {
    }
}

/// <summary>
///     Błąd autoryzacji wskazujący, że użytkownik nie ma uprawnień do wykonania operacji.
///     Tłumaczony na status HTTP 403 Forbidden.
/// </summary>
public class ForbiddenError : Error
{
    public ForbiddenError(string message) : base(message)
    {
    }
}

/// <summary>
///     Błąd uwierzytelniania wskazujący, że użytkownik nie jest zalogowany lub token jest nieprawidłowy.
///     Tłumaczony na status HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedError : Error
{
    public UnauthorizedError(string message) : base(message)
    {
    }
}
