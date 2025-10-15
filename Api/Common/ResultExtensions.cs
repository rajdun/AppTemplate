using Application.Common.Errors;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Api.Common;

public static class ResultExtensions
{
    public static IResult ToHttpResult<TValue>(this Result<TValue> result)
    {
        if (result.IsSuccess)
        {
            if (typeof(TValue) == typeof(bool) || typeof(TValue) == typeof(ValueTask))
            {
                return TypedResults.NoContent();
            }
            return TypedResults.Ok(result.Value);
        }

        return MapToProblemDetails(result.ToResult());
    }
    
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        return MapToProblemDetails(result);
    }
    
    private static IResult MapToProblemDetails(ResultBase result)
    {
        var firstError = result.Errors.FirstOrDefault();

        var errorsDict = result.Errors
            .GroupBy(e => (string?)e.Metadata.GetValueOrDefault("PropertyName", "GeneralErrors"))
            .ToDictionary(
                g => g.Key ?? "GeneralErrors", 
                g => g.Select(e => e.Message).ToArray()
            );

        switch (firstError)
        {
            case NotFoundError:
                return CreateProblemResult(
                    StatusCodes.Status404NotFound, 
                    "Not Found", 
                    firstError.Message);

            case ConflictError:
                return CreateProblemResult(
                    StatusCodes.Status409Conflict, 
                    "Conflict", 
                    firstError.Message);
            
            case ForbiddenError:
                 return CreateProblemResult(
                    StatusCodes.Status403Forbidden, 
                    "Forbidden", 
                    firstError.Message);
            case ValidationError:
                var validationProblem = new ValidationProblemDetails(errorsDict)
                {
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "See the errors property for details."
                };
                return Results.ValidationProblem(
                    validationProblem.Errors, 
                    statusCode: validationProblem.Status,
                    title: validationProblem.Title
                );
            default:
                return CreateProblemResult(
                    StatusCodes.Status400BadRequest, 
                    "An error occurred", 
                    firstError?.Message ?? "An unknown error occurred.");
        }
    }
    
    private static IResult CreateProblemResult(int statusCode, string title, string detail)
    {
        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail
        );
    }
}