using AnswerMe.Application.Common;
using FluentAssertions;

namespace AnswerMe.Tests.Common;

/// <summary>
/// 批量操作限制常量测试
/// </summary>
public class BatchLimitsTests
{
    [Fact]
    public void BatchLimits_MaxBatchCreateSize_ShouldBePositive()
    {
        // Assert
        BatchLimits.MaxBatchCreateSize.Should().BePositive();
    }

    [Fact]
    public void BatchLimits_MaxBatchDeleteSize_ShouldBePositive()
    {
        // Assert
        BatchLimits.MaxBatchDeleteSize.Should().BePositive();
    }

    [Fact]
    public void BatchLimits_DefaultPageSize_ShouldBePositive()
    {
        // Assert
        BatchLimits.DefaultPageSize.Should().BePositive();
    }

    [Fact]
    public void BatchLimits_MaxPageSize_ShouldBeGreaterThanDefault()
    {
        // Assert
        BatchLimits.MaxPageSize.Should().BeGreaterThan(BatchLimits.DefaultPageSize);
    }

    [Fact]
    public void BatchLimits_MaxSearchResults_ShouldBePositive()
    {
        // Assert
        BatchLimits.MaxSearchResults.Should().BePositive();
    }

    [Fact]
    public void BatchLimits_MaxBatchCreateSize_ShouldBe100()
    {
        // Assert
        BatchLimits.MaxBatchCreateSize.Should().Be(100);
    }

    [Fact]
    public void BatchLimits_MaxBatchDeleteSize_ShouldBe100()
    {
        // Assert
        BatchLimits.MaxBatchDeleteSize.Should().Be(100);
    }

    [Fact]
    public void BatchLimits_MaxSearchResults_ShouldBe500()
    {
        // Assert
        BatchLimits.MaxSearchResults.Should().Be(500);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(0, true)]
    public void BatchLimits_CreateBatchValidation_ShouldWork(int count, bool expectedValid)
    {
        // Arrange
        var isValid = count <= BatchLimits.MaxBatchCreateSize;

        // Assert
        isValid.Should().Be(expectedValid);
    }

    [Theory]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(0, true)]
    public void BatchLimits_DeleteBatchValidation_ShouldWork(int count, bool expectedValid)
    {
        // Arrange
        var isValid = count <= BatchLimits.MaxBatchDeleteSize;

        // Assert
        isValid.Should().Be(expectedValid);
    }
}
