using AnswerMe.Domain.Common;
using FluentAssertions;

namespace AnswerMe.Tests.Common;

/// <summary>
/// LIKE 模式转义帮助类测试
/// </summary>
public class LikePatternHelperTests
{
    [Theory]
    [InlineData("test", "%test%")]
    [InlineData("hello world", "%hello world%")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void EscapeLikePattern_NormalInput_ShouldAddWildcards(string? input, string expected)
    {
        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("%", "%[%]%")]
    [InlineData("100%", "%100[%]%")]
    [InlineData("%test%", "%[%]test[%]%")]
    [InlineData("test%value", "%test[%]value%")]
    public void EscapeLikePattern_WithPercentSign_ShouldEscape(string input, string expected)
    {
        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("_", "%[_]%")]
    [InlineData("a_b", "%a[_]b%")]
    [InlineData("test_value", "%test[_]value%")]
    public void EscapeLikePattern_WithUnderscore_ShouldEscape(string input, string expected)
    {
        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("[", "%[[]%")]
    [InlineData("[test]", "%[[]test]%")]
    [InlineData("a[b]c", "%a[[]b]c%")]
    public void EscapeLikePattern_WithBracket_ShouldEscape(string input, string expected)
    {
        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void EscapeLikePattern_WithMultipleSpecialChars_ShouldEscapeAll()
    {
        // Arrange
        var input = "test%value_other[thing]";

        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be("%test[%]value[_]other[[]thing]%");
    }

    [Fact]
    public void EscapeLikePattern_WithSqlInjectionAttempt_ShouldEscape()
    {
        // Arrange - 模拟 SQL 注入尝试
        var input = "'; DROP TABLE Users; --";

        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be("%'; DROP TABLE Users; --%");
        // 注意：这不是完整的 SQL 注入防护，但 LIKE 特殊字符已被转义
        // EF Core 的参数化查询会处理其他情况
    }

    [Fact]
    public void EscapeLikePattern_WithChineseCharacters_ShouldWork()
    {
        // Arrange
        var input = "测试搜索";

        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be("%测试搜索%");
    }

    [Fact]
    public void EscapeLikePattern_WithMixedContent_ShouldWork()
    {
        // Arrange
        var input = "Python_3.10%";

        // Act
        var result = LikePatternHelper.EscapeLikePattern(input);

        // Assert
        result.Should().Be("%Python[_]3.10[%]%");
    }
}
