using AnswerMe.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AnswerMe.UnitTests.Enums;

/// <summary>
/// QuestionType 枚举和扩展方法的单元测试
/// 测试覆盖率目标: 100%
/// </summary>
public class QuestionTypeTests
{
    #region DisplayName Tests

    [Theory]
    [InlineData(QuestionType.SingleChoice, "单选题")]
    [InlineData(QuestionType.MultipleChoice, "多选题")]
    [InlineData(QuestionType.TrueFalse, "判断题")]
    [InlineData(QuestionType.FillBlank, "填空题")]
    [InlineData(QuestionType.ShortAnswer, "简答题")]
    public void DisplayName_ShouldReturnCorrectChineseDisplayName(QuestionType type, string expected)
    {
        // Act
        var result = type.DisplayName();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void DisplayName_ShouldThrowForInvalidValue()
    {
        // Arrange
        var invalidType = (QuestionType)999;

        // Act
        var act = () => invalidType.DisplayName();

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithMessage("*未知的题型*");
    }

    #endregion

    #region ToAiPrompt Tests

    [Theory]
    [InlineData(QuestionType.SingleChoice, "single_choice")]
    [InlineData(QuestionType.MultipleChoice, "multiple_choice")]
    [InlineData(QuestionType.TrueFalse, "true_false")]
    [InlineData(QuestionType.FillBlank, "fill_blank")]
    [InlineData(QuestionType.ShortAnswer, "short_answer")]
    public void ToAiPrompt_ShouldReturnCorrectPromptFormat(QuestionType type, string expected)
    {
        // Act
        var result = type.ToAiPrompt();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToAiPrompt_ShouldThrowForInvalidValue()
    {
        // Arrange
        var invalidType = (QuestionType)999;

        // Act
        var act = () => invalidType.ToAiPrompt();

        // Assert
        act.Should().ThrowExactly<ArgumentOutOfRangeException>()
            .WithMessage("*未知的题型*");
    }

    #endregion

    #region ParseFromString Tests - Standard Enum Names

    [Theory]
    [InlineData("SingleChoice", QuestionType.SingleChoice)]
    [InlineData("MultipleChoice", QuestionType.MultipleChoice)]
    [InlineData("TrueFalse", QuestionType.TrueFalse)]
    [InlineData("FillBlank", QuestionType.FillBlank)]
    [InlineData("ShortAnswer", QuestionType.ShortAnswer)]
    public void ParseFromString_ShouldParseStandardEnumNames(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("singlechoice")]
    [InlineData("SINGLECHOICE")]
    [InlineData("SingleChoice")]
    public void ParseFromString_ShouldBeCaseInsensitive(string value)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(QuestionType.SingleChoice);
    }

    #endregion

    #region ParseFromString Tests - Legacy Format Compatibility

    [Theory]
    [InlineData("choice", QuestionType.SingleChoice)]
    [InlineData("single", QuestionType.SingleChoice)]
    [InlineData("single-choice", QuestionType.SingleChoice)]
    public void ParseFromString_ShouldMapLegacySingleChoiceFormats(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("multiple", QuestionType.MultipleChoice)]
    [InlineData("multiple-choice", QuestionType.MultipleChoice)]
    [InlineData("多选题", QuestionType.MultipleChoice)]
    public void ParseFromString_ShouldMapLegacyMultipleChoiceFormats(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("true-false", QuestionType.TrueFalse)]
    [InlineData("boolean", QuestionType.TrueFalse)]
    [InlineData("bool", QuestionType.TrueFalse)]
    [InlineData("判断题", QuestionType.TrueFalse)]
    public void ParseFromString_ShouldMapLegacyTrueFalseFormats(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("fill", QuestionType.FillBlank)]
    [InlineData("fill-blank", QuestionType.FillBlank)]
    [InlineData("填空题", QuestionType.FillBlank)]
    public void ParseFromString_ShouldMapLegacyFillBlankFormats(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("essay", QuestionType.ShortAnswer)]
    [InlineData("short-answer", QuestionType.ShortAnswer)]
    [InlineData("简答题", QuestionType.ShortAnswer)]
    public void ParseFromString_ShouldMapLegacyShortAnswerFormats(string value, QuestionType expected)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ParseFromString Tests - Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void ParseFromString_ShouldReturnNullForNullOrWhitespace(string? value)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("invalid-type")]
    [InlineData("unknown")]
    [InlineData("RandomText")]
    [InlineData("12345")]
    public void ParseFromString_ShouldReturnNullForInvalidValues(string value)
    {
        // Act
        var result = QuestionTypeExtensions.ParseFromString(value);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Enum Completeness Tests

    [Fact]
    public void QuestionType_ShouldHaveExactlyFiveValues()
    {
        // Arrange
        var expectedCount = 5;

        // Act
        var actualCount = Enum.GetValues<QuestionType>().Length;

        // Assert
        actualCount.Should().Be(expectedCount);
    }

    [Fact]
    public void QuestionType_AllValuesShouldHaveValidDisplayName()
    {
        // Arrange
        var allTypes = Enum.GetValues<QuestionType>();

        // Act & Assert
        foreach (var type in allTypes)
        {
            var act = () => type.DisplayName();
            act.Should().NotThrow($"题型 {type} 应该有有效的显示名称");
        }
    }

    [Fact]
    public void QuestionType_AllValuesShouldHaveValidAiPrompt()
    {
        // Arrange
        var allTypes = Enum.GetValues<QuestionType>();

        // Act & Assert
        foreach (var type in allTypes)
        {
            var act = () => type.ToAiPrompt();
            act.Should().NotThrow($"题型 {type} 应该有有效的 AI prompt 格式");
        }
    }

    [Fact]
    public void QuestionType_AllDisplayNamesShouldBeUnique()
    {
        // Arrange
        var allTypes = Enum.GetValues<QuestionType>();

        // Act
        var displayNames = allTypes.Select(t => t.DisplayName()).ToList();

        // Assert
        displayNames.Should().OnlyHaveUniqueItems("显示名称应该唯一");
    }

    [Fact]
    public void QuestionType_AllAiPromptsShouldBeUnique()
    {
        // Arrange
        var allTypes = Enum.GetValues<QuestionType>();

        // Act
        var aiPrompts = allTypes.Select(t => t.ToAiPrompt()).ToList();

        // Assert
        aiPrompts.Should().OnlyHaveUniqueItems("AI prompt 格式应该唯一");
    }

    #endregion
}
