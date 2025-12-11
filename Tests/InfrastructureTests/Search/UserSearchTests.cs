using Application.Common.Dto;
using Application.Common.Search.Dto.User;
using Infrastructure.Search;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InfrastructureTests.Search;

public class UserSearchTests
{
    [Fact]
    public async Task SearchUsersAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<UserSearch>>();
        var meilisearchClient = Substitute.For<Meilisearch.MeilisearchClient>("http://localhost:7700", "testKey");
        var sut = new UserSearch(logger, meilisearchClient);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await sut.SearchUsersAsync(null!));
    }

    [Fact]
    public void ApplyNameFilters_WithIsEqual_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { IsEqual = "Jan Kowalski" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name = 'Jan Kowalski'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithIsNotEqual_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { IsNotEqual = "Admin" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name != 'Admin'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithContains_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { Contains = "Kowal" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name CONTAINS 'Kowal'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithStartsWith_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { StartsWith = "Jan" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name STARTS WITH 'Jan'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithInArray_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { InArray = new List<string> { "Jan Kowalski", "Piotr Nowak" } }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name IN ['Jan Kowalski', 'Piotr Nowak']", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithNotInArray_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { NotInArray = new List<string> { "Admin", "System" } }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name NOT IN ['Admin', 'System']", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyNameFilters_WithIsNullTrue_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { IsNull = true }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Equal("name IS NULL", filters[0]);
    }

    [Fact]
    public void ApplyNameFilters_WithIsNullFalse_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { IsNull = false }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Equal("name IS NOT NULL", filters[0]);
    }

    [Fact]
    public void ApplyNameFilters_WithEmptyInArray_ShouldNotAddFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { InArray = new List<string>() }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Empty(filters);
    }

    [Fact]
    public void ApplyNameFilters_WithInArrayContainingEmptyStrings_ShouldFilterThem()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField { InArray = new List<string> { "Jan", "", "  ", "Piotr" } }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("name IN ['Jan', 'Piotr']", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyEmailFilters_WithIsEqual_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Email = new StringFilterField { IsEqual = "test@example.com" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyEmailFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("email = 'test@example.com'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyEmailFilters_WithContains_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Email = new StringFilterField { Contains = "@example.com" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyEmailFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("email CONTAINS '@example.com'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyEmailFilters_WithStartsWith_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Email = new StringFilterField { StartsWith = "admin" }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyEmailFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("email STARTS WITH 'admin'", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void ApplyEmailFilters_WithInArray_ShouldAddCorrectFilter()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Email = new StringFilterField { InArray = new List<string> { "test1@example.com", "test2@example.com" } }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyEmailFilters(request, filters);

        // Assert
        Assert.Single(filters);
        Assert.Contains("email IN ['test1@example.com', 'test2@example.com']", filters[0], StringComparison.InvariantCulture);
    }

    [Fact]
    public void EscapeFilterValue_WithSingleQuote_ShouldEscapeCorrectly()
    {
        // Arrange
        var input = "O'Brien";

        // Act
        var result = InvokeEscapeFilterValue(input);

        // Assert
        Assert.Equal("O\\'Brien", result);
    }

    [Fact]
    public void EscapeFilterValue_WithMultipleSingleQuotes_ShouldEscapeAll()
    {
        // Arrange
        var input = "It's a test's value";

        // Act
        var result = InvokeEscapeFilterValue(input);

        // Assert
        Assert.Equal("It\\'s a test\\'s value", result);
    }

    [Fact]
    public void EscapeFilterValue_WithNoSpecialCharacters_ShouldReturnUnchanged()
    {
        // Arrange
        var input = "Simple text";

        // Act
        var result = InvokeEscapeFilterValue(input);

        // Assert
        Assert.Equal("Simple text", result);
    }

    [Fact]
    public void ApplyNameFilters_WithMultipleFilters_ShouldAddAllFilters()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Name = new StringFilterField
            {
                Contains = "Jan",
                IsNotEqual = "Admin"
            }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyNameFilters(request, filters);

        // Assert
        Assert.Equal(2, filters.Count);
        Assert.Contains(filters, f => f.Contains("name != 'Admin'", StringComparison.InvariantCulture));
        Assert.Contains(filters, f => f.Contains("name CONTAINS 'Jan'", StringComparison.InvariantCulture));
    }

    [Fact]
    public void ApplyEmailFilters_WithMultipleFilters_ShouldAddAllFilters()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            Email = new StringFilterField
            {
                Contains = "@example.com",
                StartsWith = "admin"
            }
        };
        var filters = new List<string>();

        // Act
        InvokeApplyEmailFilters(request, filters);

        // Assert
        Assert.Equal(2, filters.Count);
        Assert.Contains(filters, f => f.Contains("email CONTAINS '@example.com'", StringComparison.InvariantCulture));
        Assert.Contains(filters, f => f.Contains("email STARTS WITH 'admin'", StringComparison.InvariantCulture));
    }

    // Helper methods to invoke private static methods using reflection
    private static void InvokeApplyNameFilters(PagedUserRequest request, List<string> filters)
    {
        var method = typeof(UserSearch).GetMethod("ApplyNameFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method?.Invoke(null, new object[] { request, filters });
    }

    private static void InvokeApplyEmailFilters(PagedUserRequest request, List<string> filters)
    {
        var method = typeof(UserSearch).GetMethod("ApplyEmailFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        method?.Invoke(null, new object[] { request, filters });
    }

    private static string InvokeEscapeFilterValue(string value)
    {
        var method = typeof(UserSearch).GetMethod("EscapeFilterValue",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (string)method?.Invoke(null, new object[] { value })!;
    }
}
