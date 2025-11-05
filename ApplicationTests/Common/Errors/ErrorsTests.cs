using Application.Common.Errors;

namespace ApplicationTests.Common.Errors;

public class ErrorsTests
{
    [Fact]
    public void NotFoundError_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Resource not found";

        // Act
        var error = new NotFoundError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.IsType<NotFoundError>(error);
    }

    [Fact]
    public void ConflictError_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Resource already exists";

        // Act
        var error = new ConflictError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.IsType<ConflictError>(error);
    }

    [Fact]
    public void ValidationError_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var error = new ValidationError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.IsType<ValidationError>(error);
    }

    [Fact]
    public void ForbiddenError_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Access forbidden";

        // Act
        var error = new ForbiddenError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.IsType<ForbiddenError>(error);
    }

    [Fact]
    public void UnauthorizedError_ShouldCreateWithMessage()
    {
        // Arrange
        var message = "Unauthorized access";

        // Act
        var error = new UnauthorizedError(message);

        // Assert
        Assert.Equal(message, error.Message);
        Assert.IsType<UnauthorizedError>(error);
    }

    [Fact]
    public void AllErrors_ShouldInheritFromError()
    {
        // Arrange & Act
        var notFoundError = new NotFoundError("test");
        var conflictError = new ConflictError("test");
        var validationError = new ValidationError("test");
        var forbiddenError = new ForbiddenError("test");
        var unauthorizedError = new UnauthorizedError("test");

        // Assert
        Assert.IsAssignableFrom<FluentResults.Error>(notFoundError);
        Assert.IsAssignableFrom<FluentResults.Error>(conflictError);
        Assert.IsAssignableFrom<FluentResults.Error>(validationError);
        Assert.IsAssignableFrom<FluentResults.Error>(forbiddenError);
        Assert.IsAssignableFrom<FluentResults.Error>(unauthorizedError);
    }
}

