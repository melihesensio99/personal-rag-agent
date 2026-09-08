using TelegramAi.Backend.Application.Shared.Common.Pagination;
using Xunit;

namespace TelegramAi.Backend.Tests;

public sealed class PaginationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(2, 2)]
    public void NormalizePage_ClampsToPositiveValue(int input, int expected)
    {
        Assert.Equal(expected, PaginationDefaults.NormalizePage(input));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 20)]
    [InlineData(500, 100)]
    public void NormalizePageSize_UsesSafeBounds(int input, int expected)
    {
        Assert.Equal(expected, PaginationDefaults.NormalizePageSize(input));
    }

    [Fact]
    public void PagedResult_ExposesNavigationMetadata()
    {
        var result = new PagedResult<int>([1, 2], Page: 2, PageSize: 2, TotalCount: 5);

        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasPreviousPage);
        Assert.True(result.HasNextPage);
    }
}
