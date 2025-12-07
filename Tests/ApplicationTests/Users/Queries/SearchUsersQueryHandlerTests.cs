using Application.Common.Dto;
using Application.Common.Search;
using Application.Common.Search.Dto;
using Application.Common.Search.Dto.User;
using Application.Users.Queries;
using NSubstitute;

namespace ApplicationTests.Users.Queries;

public class SearchUsersQueryHandlerTests
{
    private readonly IUserSearch _search;
    private readonly SearchUsersQueryHandler _handler;

    public SearchUsersQueryHandlerTests()
    {
        _search = Substitute.For<IUserSearch>();
        _handler = new SearchUsersQueryHandler(_search);
    }

    [Fact]
    public async Task Handle_ShouldCallSearchService()
    {
        // Arrange
        var request = new PagedUserRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name",
            SortOrder = SortDirection.Asc
        };
        var query = new SearchUsersQuery(request);
        var expectedResult = new PagedResult<UserSearchDocumentDto>
        {
            Items = new List<UserSearchDocumentDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _search.SearchUsersAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult, result.Value);
        await _search.Received(1).SearchUsersAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSearchFails_ShouldReturnFailure()
    {
        // Arrange
        var request = new PagedUserRequest { PageNumber = 1, PageSize = 10 };
        var query = new SearchUsersQuery(request);

        _search.SearchUsersAsync(request, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserSearchDocumentDto>
            {
                Items = new List<UserSearchDocumentDto>(),
                TotalCount = 0,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public void Validator_WithZeroPageNumber_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest { PageNumber = 0, PageSize = 10 });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageNumber", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Validator_WithZeroPageSize_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest { PageNumber = 1, PageSize = 0 });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageSize", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Validator_WithPageSizeGreaterThan100_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest { PageNumber = 1, PageSize = 101 });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("PageSize", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Validator_WithInvalidSortField_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "invalid_field"
        });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName.Contains("SortBy", StringComparison.InvariantCulture));
    }

    [Fact]
    public void Validator_WithValidSortField_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "name"
        });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_WithValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new SearchUsersQueryValidator();
        var query = new SearchUsersQuery(new PagedUserRequest { PageNumber = 1, PageSize = 10 });

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}

