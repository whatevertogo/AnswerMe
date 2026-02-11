using System.Text.Json;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Xunit;

namespace AnswerMe.UnitTests.Compatibility;

/// <summary>
/// 向后兼容性测试
/// 验证新旧字段并存时现有功能不受影响
/// </summary>
public class BackwardCompatibilityTests
{
    private readonly JsonSerializerOptions _jsonOptions;

    public BackwardCompatibilityTests()
    {
        _jsonOptions = QuestionDataJsonOptions.Default;
    }

    #region Question Entity - New/Old Fields Coexistence

    [Fact]
    public void Question_ShouldSupportOldFieldsAccess()
    {
        // Arrange & Act - 使用旧字段创建题目
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\", \"C. 选项3\", \"D. 选项4\"]",
            CorrectAnswer = "A",
            Explanation = "这是解析",
            Difficulty = "easy"
        };

        // Assert - 验证旧字段仍然可访问
        question.Options.Should().Be("[\"A. 选项1\", \"B. 选项2\", \"C. 选项3\", \"D. 选项4\"]");
        question.CorrectAnswer.Should().Be("A");
        question.QuestionType.Should().Be("choice");
    }

    [Fact]
    public void Question_ShouldSupportNewFieldsAccess()
    {
        // Arrange & Act - 使用新字段创建题目
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            QuestionTypeEnum = QuestionType.MultipleChoice,
            Data = new ChoiceQuestionData
            {
                Options = new List<string> { "A. 选项1", "B. 选项2" },
                CorrectAnswers = new List<string> { "A", "B" },
                Explanation = "多选题解析",
                Difficulty = "medium"
            }
        };

        // Assert - 验证新字段正常工作
        question.QuestionTypeEnum.Should().Be(QuestionType.MultipleChoice);
        question.Data.Should().NotBeNull();
        question.Data.Should().BeOfType<ChoiceQuestionData>();

        var choiceData = question.Data as ChoiceQuestionData;
        choiceData!.Options.Should().HaveCount(2);
        choiceData.CorrectAnswers.Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public void Question_NewAndOldFields_ShouldCoexist()
    {
        // Arrange - 同时设置新旧字段
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            // 旧字段
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\"]",
            CorrectAnswer = "A",
            // 新字段
            QuestionTypeEnum = QuestionType.SingleChoice,
            Data = new ChoiceQuestionData
            {
                Options = new List<string> { "A. 新选项1", "B. 新选项2" },
                CorrectAnswers = new List<string> { "A" },
                Explanation = "新格式解析",
                Difficulty = "hard"
            }
        };

        // Assert - 验证两者都可以独立访问
        // 旧字段
        question.Options.Should().Be("[\"A. 选项1\", \"B. 选项2\"]");
        question.CorrectAnswer.Should().Be("A");
        question.QuestionType.Should().Be("SingleChoice"); // 被 QuestionTypeEnum 覆盖

        // 新字段
        question.QuestionTypeEnum.Should().Be(QuestionType.SingleChoice);
        question.Data.Should().NotBeNull();
        var data = question.Data as ChoiceQuestionData;
        data!.Options.Should().BeEquivalentTo(new[] { "A. 新选项1", "B. 新选项2" });
    }

    #endregion

    #region QuestionType String to Enum Mapping

    [Theory]
    [InlineData("SingleChoice", QuestionType.SingleChoice)]
    [InlineData("MultipleChoice", QuestionType.MultipleChoice)]
    [InlineData("TrueFalse", QuestionType.TrueFalse)]
    [InlineData("FillBlank", QuestionType.FillBlank)]
    [InlineData("ShortAnswer", QuestionType.ShortAnswer)]
    public void QuestionTypeEnum_ShouldMapFromEnumString(string enumString, QuestionType expected)
    {
        // Arrange
        var question = new Question { QuestionType = enumString };

        // Act
        var result = question.QuestionTypeEnum;

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("choice", QuestionType.SingleChoice)]
    [InlineData("single", QuestionType.SingleChoice)]
    [InlineData("multiple", QuestionType.MultipleChoice)]
    [InlineData("true-false", QuestionType.TrueFalse)]
    [InlineData("fill", QuestionType.FillBlank)]
    [InlineData("essay", QuestionType.ShortAnswer)]
    public void QuestionTypeEnum_ShouldMapFromLegacyStrings(string legacyString, QuestionType expected)
    {
        // Arrange
        var question = new Question { QuestionType = legacyString };

        // Act
        var result = question.QuestionTypeEnum;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void QuestionTypeEnum_Setter_ShouldUpdateQuestionTypeString()
    {
        // Arrange
        var question = new Question();

        // Act
        question.QuestionTypeEnum = QuestionType.MultipleChoice;

        // Assert
        question.QuestionType.Should().Be("MultipleChoice");
    }

    [Fact]
    public void QuestionTypeEnum_SetterWithNull_ShouldSetToEmptyString()
    {
        // Arrange
        var question = new Question();

        // Act
        question.QuestionTypeEnum = null;

        // Assert
        question.QuestionType.Should().Be(string.Empty);
    }

    #endregion

    #region QuestionData JSON Serialization

    [Fact]
    public void Data_ShouldSerializeToJsonWithCorrectFormat()
    {
        // Arrange
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            Data = new ChoiceQuestionData
            {
                Options = new List<string> { "A. 选项1", "B. 选项2" },
                CorrectAnswers = new List<string> { "A" },
                Explanation = "解析",
                Difficulty = "easy"
            }
        };

        // Act
        var json = question.QuestionDataJson;

        // Assert
        json.Should().NotBeNull();
        json.Should().Contain("\"type\":\"choice\"");
        using var doc = JsonDocument.Parse(json);
        var options = doc.RootElement.GetProperty("options").EnumerateArray().Select(x => x.GetString()).ToList();
        options.Should().BeEquivalentTo(new[] { "A. 选项1", "B. 选项2" });
        var answers = doc.RootElement.GetProperty("correctAnswers").EnumerateArray().Select(x => x.GetString()).ToList();
        answers.Should().BeEquivalentTo(new[] { "A" });
    }

    [Fact]
    public void Data_ShouldDeserializeFromJsonCorrectly()
    {
        // Arrange
        var json = """
            {
              "type": "choice",
              "options": ["A. 选项1", "B. 选项2", "C. 选项3"],
              "correctAnswers": ["A", "C"],
              "explanation": "多选题解析",
              "difficulty": "medium"
            }
            """;

        var question = new Question { QuestionDataJson = json };

        // Act
        var data = question.Data as ChoiceQuestionData;

        // Assert
        data.Should().NotBeNull();
        data!.Options.Should().HaveCount(3);
        data.CorrectAnswers.Should().BeEquivalentTo(new[] { "A", "C" });
        data.Explanation.Should().Be("多选题解析");
        data.Difficulty.Should().Be("medium");
    }

    [Fact]
    public void Data_ShouldHandleInvalidJsonGracefully()
    {
        // Arrange
        var question = new Question
        {
            QuestionDataJson = "{invalid json content"
        };

        // Act
        var data = question.Data;

        // Assert
        data.Should().BeNull("损坏的 JSON 应该返回 null 而非抛异常");
    }

    [Fact]
    public void Data_ShouldReturnNullForEmptyJson()
    {
        // Arrange
        var question = new Question { QuestionDataJson = "" };

        // Act
        var data = question.Data;

        // Assert
        data.Should().BeNull();
    }

    [Fact]
    public void Data_ShouldReturnNullForWhitespaceJson()
    {
        // Arrange
        var question = new Question { QuestionDataJson = "   " };

        // Act
        var data = question.Data;

        // Assert
        data.Should().BeNull();
    }

    #endregion

    #region ChoiceQuestionData Null Safety

    [Fact]
    public void ChoiceQuestionData_Options_ShouldHandleNullAssignment()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. Test" },
            CorrectAnswers = new List<string> { "A" }
        };

        // Act
        data.Options = null!;

        // Assert
        data.Options.Should().NotBeNull();
        data.Options.Should().BeEmpty();
    }

    [Fact]
    public void ChoiceQuestionData_CorrectAnswers_ShouldHandleNullAssignment()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. Test" },
            CorrectAnswers = new List<string> { "A" }
        };

        // Act
        data.CorrectAnswers = null!;

        // Assert
        data.CorrectAnswers.Should().NotBeNull();
        data.CorrectAnswers.Should().BeEmpty();
    }

    [Fact]
    public void ChoiceQuestionData_ShouldDeserializeJsonWithNullLists()
    {
        // Arrange
        var json = """
            {
              "type": "choice",
              "options": null,
              "correctAnswers": null,
              "difficulty": "easy"
            }
            """;

        // Act
        var data = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _jsonOptions);

        // Assert
        data.Should().NotBeNull();
        data.Options.Should().NotBeNull("Options 应该自动初始化为空列表");
        data.Options.Should().BeEmpty();
        data.CorrectAnswers.Should().NotBeNull("CorrectAnswers 应该自动初始化为空列表");
        data.CorrectAnswers.Should().BeEmpty();
    }

    #endregion

    #region Round-Trip Serialization Tests

    [Fact]
    public void QuestionData_ShouldSurviveSerializationRoundTrip()
    {
        // Arrange
        var original = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
            CorrectAnswers = new List<string> { "B", "C", "D" }, // 多选题
            Explanation = "这是详细解析内容",
            Difficulty = "hard"
        };

        // Act - 序列化
        var question = new Question { Data = original };
        var json = question.QuestionDataJson;

        // 反序列化
        var question2 = new Question { QuestionDataJson = json };
        var restored = question2.Data as ChoiceQuestionData;

        // Assert
        restored.Should().NotBeNull();
        restored!.Should().BeEquivalentTo(original);
        restored.Options.Should().BeEquivalentTo(original.Options);
        restored.CorrectAnswers.Should().BeEquivalentTo(original.CorrectAnswers);
    }

    [Fact]
    public void QuestionData_BooleanQuestionData_ShouldSurviveRoundTrip()
    {
        // Arrange
        var original = new BooleanQuestionData
        {
            CorrectAnswer = true,
            Explanation = "这是正确的",
            Difficulty = "easy"
        };

        // Act
        var question = new Question { Data = original };
        var json = question.QuestionDataJson;
        var question2 = new Question { QuestionDataJson = json };
        var restored = question2.Data as BooleanQuestionData;

        // Assert
        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void QuestionData_FillBlankQuestionData_ShouldSurviveRoundTrip()
    {
        // Arrange
        var original = new FillBlankQuestionData
        {
            AcceptableAnswers = new List<string> { "北京", "Beijing", "beijing" },
            Explanation = "首都",
            Difficulty = "medium"
        };

        // Act
        var question = new Question { Data = original };
        var json = question.QuestionDataJson;
        var question2 = new Question { QuestionDataJson = json };
        var restored = question2.Data as FillBlankQuestionData;

        // Assert
        restored.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void QuestionData_ShortAnswerQuestionData_ShouldSurviveRoundTrip()
    {
        // Arrange
        var original = new ShortAnswerQuestionData
        {
            ReferenceAnswer = "这是参考答案，可以很长很详细",
            Explanation = "简答题解析",
            Difficulty = "hard"
        };

        // Act
        var question = new Question { Data = original };
        var json = question.QuestionDataJson;
        var question2 = new Question { QuestionDataJson = json };
        var restored = question2.Data as ShortAnswerQuestionData;

        // Assert
        restored.Should().BeEquivalentTo(original);
    }

    #endregion

    #region Obsolete Attributes Warning Tests

    [Fact]
    public void ObsoleteFields_ShouldStillBeAccessible()
    {
        // Arrange & Act
        var question = new Question
        {
            Options = "[\"A. Test\"]",
            CorrectAnswer = "A"
        };

        // Assert - 验证 Obsolete 字段仍然可用
        question.Options.Should().Be("[\"A. Test\"]");
        question.CorrectAnswer.Should().Be("A");

        // 注意：编译器会显示警告，但运行时不受影响
    }

    [Fact]
    public void ObsoleteFields_CanBeReadFromOldCode()
    {
        // Arrange - 模拟旧代码访问旧字段
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "旧格式的题目",
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\"]",
            CorrectAnswer = "A",
            Explanation = "旧格式解析",
            Difficulty = "medium"
        };

        // Act - 模拟旧代码读取
        var oldOptions = question.Options;
        var oldAnswer = question.CorrectAnswer;
        var oldType = question.QuestionType;

        // Assert - 验证旧代码仍然工作
        oldOptions.Should().Be("[\"A. 选项1\", \"B. 选项2\"]");
        oldAnswer.Should().Be("A");
        oldType.Should().Be("choice");
    }

    #endregion

    #region Migration Path Tests

    [Fact]
    public void Migration_OldFormatToNewFormat_ShouldWork()
    {
        // Arrange - 旧格式数据
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "迁移测试题目",
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\", \"C. 选项3\", \"D. 选项4\"]",
            CorrectAnswer = "B",
            Explanation = "旧格式解析",
            Difficulty = "easy"
        };

        // Act - 迁移到新格式
        oldQuestion.QuestionTypeEnum = QuestionType.SingleChoice;
        oldQuestion.Data = new ChoiceQuestionData
        {
            Options = JsonSerializer.Deserialize<List<string>>(oldQuestion.Options!)!,
            CorrectAnswers = new List<string> { oldQuestion.CorrectAnswer },
            Explanation = oldQuestion.Explanation,
            Difficulty = oldQuestion.Difficulty
        };

        // Assert - 验证迁移成功
        oldQuestion.QuestionTypeEnum.Should().Be(QuestionType.SingleChoice);
        oldQuestion.Data.Should().BeOfType<ChoiceQuestionData>();

        var newData = oldQuestion.Data as ChoiceQuestionData;
        newData!.Options.Should().HaveCount(4);
        newData.CorrectAnswers.Should().ContainSingle();
        newData.CorrectAnswers[0].Should().Be("B");
        newData.Explanation.Should().Be("旧格式解析");
    }

    [Fact]
    public void Migration_PreservesOldDataWhenNewFormatSet()
    {
        // Arrange - 旧格式数据
        var originalOptions = "[\"A. 选项1\", \"B. 选项2\"]";
        var originalAnswer = "A";

        var question = new Question
        {
            Options = originalOptions,
            CorrectAnswer = originalAnswer
        };

        // Act - 设置新格式
        question.Data = new ChoiceQuestionData
        {
            Options = new List<string> { "C. 新选项1", "D. 新选项2" },
            CorrectAnswers = new List<string> { "C" }
        };

        // Assert - 验证旧数据仍然存在 (未丢失)
        question.Options.Should().Be(originalOptions);
        question.CorrectAnswer.Should().Be(originalAnswer);

        // 新数据独立存储
        var newData = question.Data as ChoiceQuestionData;
        newData!.Options.Should().BeEquivalentTo(new[] { "C. 新选项1", "D. 新选项2" });
        newData.CorrectAnswers.Should().ContainSingle("C");
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void Question_WithNullQuestionDataJson_ShouldNotThrow()
    {
        // Arrange & Act
        var question = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试",
            QuestionType = "choice",
            QuestionDataJson = null
        };

        // Assert
        question.Data.Should().BeNull();
    }

    [Fact]
    public void Question_WithInvalidQuestionTypeEnum_ShouldReturnNull()
    {
        // Arrange
        var question = new Question
        {
            QuestionType = "invalid-type-that-does-not-exist"
        };

        // Act
        var result = question.QuestionTypeEnum;

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Question_CanSetQuestionTypeStringDirectly()
    {
        // Arrange
        var question = new Question();

        // Act
        question.QuestionType = "MultipleChoice";

        // Assert
        question.QuestionType.Should().Be("MultipleChoice");
        question.QuestionTypeEnum.Should().Be(QuestionType.MultipleChoice);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void QuestionTypeEnum_WithInvalidString_ShouldReturnNull(string invalidValue)
    {
        // Arrange
        var question = new Question { QuestionType = invalidValue };

        // Act
        var result = question.QuestionTypeEnum;

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
