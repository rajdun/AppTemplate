using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Application.Users.Queries;
using FluentResults;
using NSubstitute;

namespace ApplicationTests.Users.Queries;

public class SearchUsersQueryHandlerTests
{
    private readonly IUserSearch _userSearch;

    public SearchUsersQueryHandlerTests()
    {
        _userSearch = Substitute.For<IUserSearch>();
    }

    private SearchUsersQueryHandler CreateHandler() => new(_userSearch);

    [Fact]
    public async Task Handle_WithValidRequest_ShouldDelegateToUserSearch()
    {
        // Arrange
        var request = new PagedUserRequest { PageNumber = 1, PageSize = 10 };
        var expected = new PagedResult<UserSearchDocumentDto>
        {
            Items = [new UserSearchDocumentDto(Guid.NewGuid(), "Jan Kowalski", "jan@example.com")],
            TotalCount = 1, PageNumber = 1, PageSize = 10
        };

        _userSearch.SearchUsersAsync(request, Arg.Any<CancellationToken>()).Returns(expected);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SearchUsersQuery(request));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
        await _userSearch.Received(1).SearchUsersAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ShouldReturnSuccessWithEmptyList()
    {
        // Arrange
        var request = new PagedUserRequest { PageNumber = 1, PageSize = 10 };
        var empty = new PagedResult<UserSearchDocumentDto>
        {
            Items = [], TotalCount = 0, PageNumber = 1, PageSize = 10
        };

        _userSearch.SearchUsersAsync(request, Arg.Any<CancellationToken>()).Returns(empty);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new SearchUsersQuery(request));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }
}

public class SearchUsersQueryValidatorTests
{
    private readonly SearchUsersQueryValidator _validator = new();

    private static SearchUsersQuery ValidQuery(
        int pageNumber = 1,
        int pageSize = 10,
        string? sortBy = null) =>
        new(new PagedUserRequest { PageNumber = pageNumber, PageSize = pageSize, SortBy = sortBy });

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        var result = _validator.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithPageNumberZero_ShouldFail()
    {
        var result = _validator.Validate(ValidQuery(pageNumber: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageNumber", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithNegativePageNumber_ShouldFail()
    {
        var result = _validator.Validate(ValidQuery(pageNumber: -1));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithPageSizeZero_ShouldFail()
    {
        var result = _validator.Validate(ValidQuery(pageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageSize", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithPageSizeOver100_ShouldFail()
    {
        var result = _validator.Validate(ValidQuery(pageSize: 101));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageSize", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithPageSizeExactly100_ShouldPass()
    {
        var result = _validator.Validate(ValidQuery(pageSize: 100));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithValidSortByName_ShouldPass()
    {
        var result = _validator.Validate(ValidQuery(sortBy: "Name"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithValidSortByNameCaseInsensitive_ShouldPass()
    {
        var result = _validator.Validate(ValidQuery(sortBy: "name"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidSortBy_ShouldFail()
    {
        var result = _validator.Validate(ValidQuery(sortBy: "InvalidField"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("SortBy", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WithNullSortBy_ShouldPass()
    {
        var result = _validator.Validate(ValidQuery(sortBy: null));

        Assert.True(result.IsValid);
    }
}






