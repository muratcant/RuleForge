using FluentAssertions;
using RuleForge.Application.Common;

namespace RuleForge.Tests.Common;

public sealed class PagedResultTests
{
    [Fact]
    public void TotalPages_WhenTotalCount25AndPageSize10_Returns3()
    {
        var result = new PagedResult<object>
        {
            Items = [],
            Page = 1,
            PageSize = 10,
            TotalCount = 25
        };

        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void TotalPages_WhenPageSizeZero_Returns0()
    {
        var result = new PagedResult<object>
        {
            Items = [],
            Page = 1,
            PageSize = 0,
            TotalCount = 25
        };

        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void TotalPages_WhenTotalCount20AndPageSize10_Returns2()
    {
        var result = new PagedResult<object>
        {
            Items = [],
            Page = 1,
            PageSize = 10,
            TotalCount = 20
        };

        result.TotalPages.Should().Be(2);
    }
}
