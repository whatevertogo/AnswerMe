using System.Text.Json;
using AnswerMe.Domain.Models;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Xunit;

namespace AnswerMe.UnitTests.Models;

/// <summary>
/// QuestionData 层次结构的序列化/反序列化测试
/// 测试覆盖率目标: > 90%
/// </summary>
public class QuestionDataTests
{
    private readonly JsonSerializerOptions _options;

    public QuestionDataTests()
    {
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        // 注意：使用 QuestionDataJsonOptions.Default 进行多态序列化
    }

    #region ChoiceQuestionData - Single Choice

    [Fact]
    public void ChoiceQuestionData_SerializeSingleChoice_ShouldIncludeAllFields()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
            CorrectAnswers = new List<string> { "A" },
            Explanation = "这是正确答案的解析",
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"options\"");
        json.Should().Contain("\"correctAnswers\"");
        json.Should().Contain("\"explanation\"");
        json.Should().Contain("\"difficulty\"");
        json.Should().Contain("\"$type\":\"ChoiceQuestionData\"");
    }

    [Fact]
    public void ChoiceQuestionData_DeserializeSingleChoice_ShouldRestoreCorrectly()
    {
        // Arrange
        var json = """
            {
                "$type": "ChoiceQuestionData",
                "options": ["A. JavaScript", "B. Python", "C. Java", "D. C++"],
                "correctAnswers": ["A"],
                "explanation": "JavaScript是Web开发的主要语言",
                "difficulty": "medium"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.Options.Should().HaveCount(4);
        result.Options[0].Should().Be("A. JavaScript");
        result.CorrectAnswers.Should().ContainSingle();
        result.CorrectAnswers[0].Should().Be("A");
        result.Explanation.Should().Be("JavaScript是Web开发的主要语言");
        result.Difficulty.Should().Be("medium");
    }

    [Fact]
    public void ChoiceQuestionData_RoundTripSingleChoice_ShouldPreserveAllData()
    {
        // Arrange
        var original = new ChoiceQuestionData
        {
            Options = new List<string> { "A. True", "B. False" },
            CorrectAnswers = new List<string> { "A" },
            Explanation = "Test explanation",
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    #endregion

    #region ChoiceQuestionData - Multiple Choice

    [Fact]
    public void ChoiceQuestionData_SerializeMultipleChoice_ShouldHandleMultipleAnswers()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
            CorrectAnswers = new List<string> { "A", "C", "D" }, // 多选题: 3个正确答案
            Explanation = "多选题解析",
            Difficulty = "hard"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"correctAnswers\":[\"A\",\"C\",\"D\"]");
    }

    [Fact]
    public void ChoiceQuestionData_DeserializeMultipleChoice_ShouldHandleMultipleAnswers()
    {
        // Arrange
        var json = """
            {
                "$type": "ChoiceQuestionData",
                "options": ["A. HTML", "B. C", "C. CSS", "D. Assembly"],
                "correctAnswers": ["A", "C"],
                "explanation": "HTML和CSS都是Web前端技术",
                "difficulty": "medium"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.CorrectAnswers.Should().HaveCount(2);
        result.CorrectAnswers.Should().BeEquivalentTo(new[] { "A", "C" });
    }

    [Theory]
    [InlineData(1)] // 最少1个答案
    [InlineData(2)] // 2个答案
    [InlineData(3)] // 最多3个答案
    public void ChoiceQuestionData_ShouldSupportOneToThreeCorrectAnswers(int answerCount)
    {
        // Arrange
        var answers = Enumerable.Range(0, answerCount).Select(i => ((char)('A' + i)).ToString()).ToList();
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 1", "B. 2", "C. 3", "D. 4" },
            CorrectAnswers = answers
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        deserialized!.CorrectAnswers.Should().HaveCount(answerCount);
    }

    #endregion

    #region BooleanQuestionData

    [Fact]
    public void BooleanQuestionData_SerializeTrue_ShouldWorkCorrectly()
    {
        // Arrange
        var data = new BooleanQuestionData
        {
            CorrectAnswer = true,
            Explanation = "这是正确的陈述",
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"correctAnswer\":true");
        json.Should().Contain("\"$type\":\"BooleanQuestionData\"");
    }

    [Fact]
    public void BooleanQuestionData_SerializeFalse_ShouldWorkCorrectly()
    {
        // Arrange
        var data = new BooleanQuestionData
        {
            CorrectAnswer = false,
            Explanation = "这是错误的陈述",
            Difficulty = "medium"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"correctAnswer\":false");
    }

    [Fact]
    public void BooleanQuestionData_DeserializeTrue_ShouldRestoreCorrectly()
    {
        // Arrange
        var json = """
            {
                "$type": "BooleanQuestionData",
                "correctAnswer": true,
                "explanation": "地球是圆的",
                "difficulty": "easy"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<BooleanQuestionData>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.CorrectAnswer.Should().BeTrue();
        result.Explanation.Should().Be("地球是圆的");
        result.Difficulty.Should().Be("easy");
    }

    [Fact]
    public void BooleanQuestionData_RoundTrip_ShouldPreserveBooleanValue()
    {
        // Arrange
        var original = new BooleanQuestionData
        {
            CorrectAnswer = false,
            Explanation = "Test",
            Difficulty = "hard"
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<BooleanQuestionData>(json, _options);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    #endregion

    #region FillBlankQuestionData

    [Fact]
    public void FillBlankQuestionData_Serialize_ShouldIncludeAllAcceptableAnswers()
    {
        // Arrange
        var data = new FillBlankQuestionData
        {
            AcceptableAnswers = new List<string> { "JavaScript", "JS", "javascript", "js" },
            Explanation = "JavaScript是一种脚本语言",
            Difficulty = "medium"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"acceptableAnswers\"");
        json.Should().Contain("JavaScript");
        json.Should().Contain("\"$type\":\"FillBlankQuestionData\"");
    }

    [Fact]
    public void FillBlankQuestionData_Deserialize_ShouldRestoreAllAnswers()
    {
        // Arrange
        var json = """
            {
                "$type": "FillBlankQuestionData",
                "acceptableAnswers": ["北京", "Beijing", "beijing"],
                "explanation": "中国首都是北京",
                "difficulty": "easy"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<FillBlankQuestionData>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.AcceptableAnswers.Should().HaveCount(3);
        result.AcceptableAnswers.Should().Contain("北京");
        result.AcceptableAnswers.Should().Contain("Beijing");
    }

    [Fact]
    public void FillBlankQuestionData_EmptyAnswers_ShouldSerializeCorrectly()
    {
        // Arrange
        var data = new FillBlankQuestionData
        {
            AcceptableAnswers = new List<string>(),
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<FillBlankQuestionData>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.AcceptableAnswers.Should().BeEmpty();
    }

    [Fact]
    public void FillBlankQuestionData_RoundTrip_ShouldPreserveAllAnswers()
    {
        // Arrange
        var original = new FillBlankQuestionData
        {
            AcceptableAnswers = new List<string> { "答案1", "答案2", "答案3" },
            Explanation = "多个可接受答案",
            Difficulty = "medium"
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<FillBlankQuestionData>(json, _options);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    #endregion

    #region ShortAnswerQuestionData

    [Fact]
    public void ShortAnswerQuestionData_Serialize_ShouldIncludeReferenceAnswer()
    {
        // Arrange
        var data = new ShortAnswerQuestionData
        {
            ReferenceAnswer = "这是参考答案，包含详细的解释和说明",
            Explanation = "简答题解析",
            Difficulty = "hard"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);

        // Assert
        json.Should().Contain("\"referenceAnswer\"");
        json.Should().Contain("这是参考答案");
        json.Should().Contain("\"$type\":\"ShortAnswerQuestionData\"");
    }

    [Fact]
    public void ShortAnswerQuestionData_Deserialize_ShouldRestoreReferenceAnswer()
    {
        // Arrange
        var json = """
            {
                "$type": "ShortAnswerQuestionData",
                "referenceAnswer": "HTML是一种标记语言，用于创建网页结构",
                "explanation": "HTML全称HyperText Markup Language",
                "difficulty": "medium"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<ShortAnswerQuestionData>(json, _options);

        // Assert
        result.Should().NotBeNull();
        result!.ReferenceAnswer.Should().Be("HTML是一种标记语言，用于创建网页结构");
        result.Explanation.Should().Be("HTML全称HyperText Markup Language");
        result.Difficulty.Should().Be("medium");
    }

    [Fact]
    public void ShortAnswerQuestionData_EmptyReferenceAnswer_ShouldSerializeCorrectly()
    {
        // Arrange
        var data = new ShortAnswerQuestionData
        {
            ReferenceAnswer = string.Empty,
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<ShortAnswerQuestionData>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ReferenceAnswer.Should().BeEmpty();
    }

    [Fact]
    public void ShortAnswerQuestionData_RoundTrip_ShouldPreserveContent()
    {
        // Arrange
        var original = new ShortAnswerQuestionData
        {
            ReferenceAnswer = "详细的参考答案内容，可以很长，包含多行文本和特殊符号: @#$%",
            Explanation = "测试解析",
            Difficulty = "hard"
        };

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<ShortAnswerQuestionData>(json, _options);

        // Assert
        deserialized.Should().BeEquivalentTo(original);
    }

    #endregion

    #region Polymorphic Deserialization

    [Fact]
    public void QuestionData_PolymorphicDeserialize_ChoiceQuestionData()
    {
        // Arrange
        var json = """
            {
                "$type": "ChoiceQuestionData",
                "options": ["A. 1", "B. 2"],
                "correctAnswers": ["A"],
                "difficulty": "easy"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionData>(json, QuestionDataJsonOptions.Default);

        // Assert
        result.Should().BeOfType<ChoiceQuestionData>();
        var choiceData = result as ChoiceQuestionData;
        choiceData!.Options.Should().HaveCount(2);
    }

    [Fact]
    public void QuestionData_PolymorphicDeserialize_BooleanQuestionData()
    {
        // Arrange
        var json = """
            {
                "$type": "BooleanQuestionData",
                "correctAnswer": true,
                "difficulty": "easy"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionData>(json, QuestionDataJsonOptions.Default);

        // Assert
        result.Should().BeOfType<BooleanQuestionData>();
        var booleanData = result as BooleanQuestionData;
        booleanData!.CorrectAnswer.Should().BeTrue();
    }

    [Fact]
    public void QuestionData_PolymorphicDeserialize_FillBlankQuestionData()
    {
        // Arrange
        var json = """
            {
                "$type": "FillBlankQuestionData",
                "acceptableAnswers": ["答案1"],
                "difficulty": "medium"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionData>(json, QuestionDataJsonOptions.Default);

        // Assert
        result.Should().BeOfType<FillBlankQuestionData>();
        var fillData = result as FillBlankQuestionData;
        fillData!.AcceptableAnswers.Should().ContainSingle();
    }

    [Fact]
    public void QuestionData_PolymorphicDeserialize_ShortAnswerQuestionData()
    {
        // Arrange
        var json = """
            {
                "$type": "ShortAnswerQuestionData",
                "referenceAnswer": "参考答案",
                "difficulty": "hard"
            }
            """;

        // Act
        var result = JsonSerializer.Deserialize<QuestionData>(json, QuestionDataJsonOptions.Default);

        // Assert
        result.Should().BeOfType<ShortAnswerQuestionData>();
        var shortData = result as ShortAnswerQuestionData;
        shortData!.ReferenceAnswer.Should().Be("参考答案");
    }

    #endregion

    #region Edge Cases and Special Characters

    [Fact]
    public void QuestionData_ShouldHandleChineseCharacters()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 选项一", "B. 选项二", "C. 选项三" },
            CorrectAnswers = new List<string> { "A" },
            Explanation = "这是中文解析，包含特殊字符：！@#¥%……&*（）",
            Difficulty = "medium"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Explanation.Should().Contain("中文解析");
        deserialized.Explanation.Should().Contain("！@#¥%");
    }

    [Fact]
    public void QuestionData_ShouldHandleJsonSpecialCharacters()
    {
        // Arrange
        var data = new ShortAnswerQuestionData
        {
            ReferenceAnswer = "包含引号\"和换行\n以及制表符\t的内容",
            Difficulty = "medium"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<ShortAnswerQuestionData>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ReferenceAnswer.Should().Contain("包含引号");
        deserialized.ReferenceAnswer.Should().Contain("\n");
        deserialized.ReferenceAnswer.Should().Contain("\t");
    }

    [Fact]
    public void QuestionData_ShouldHandleNullValues()
    {
        // Arrange
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. Test" },
            CorrectAnswers = new List<string> { "A" },
            Explanation = null,
            Difficulty = "easy"
        };

        // Act
        var json = JsonSerializer.Serialize(data, _options);
        var deserialized = JsonSerializer.Deserialize<ChoiceQuestionData>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Explanation.Should().BeNull();
    }

    [Fact]
    public void QuestionData_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var choiceData = new ChoiceQuestionData();
        var booleanData = new BooleanQuestionData();
        var fillBlankData = new FillBlankQuestionData();
        var shortAnswerData = new ShortAnswerQuestionData();

        // Assert
        choiceData.Options.Should().BeEmpty();
        choiceData.CorrectAnswers.Should().BeEmpty();

        booleanData.CorrectAnswer.Should().BeFalse(); // bool 默认值

        fillBlankData.AcceptableAnswers.Should().BeEmpty();

        shortAnswerData.ReferenceAnswer.Should().BeEmpty();
    }

    #endregion

    #region Inheritance and Base Class Properties

    [Fact]
    public void QuestionData_AllTypes_ShouldInheritBaseProperties()
    {
        // Arrange
        var choice = new ChoiceQuestionData { Difficulty = "easy" };
        var boolean = new BooleanQuestionData { Difficulty = "medium" };
        var fillBlank = new FillBlankQuestionData { Difficulty = "hard" };
        var shortAnswer = new ShortAnswerQuestionData { Difficulty = "medium" };

        // Act & Assert
        choice.Difficulty.Should().Be("easy");
        boolean.Difficulty.Should().Be("medium");
        fillBlank.Difficulty.Should().Be("hard");
        shortAnswer.Difficulty.Should().Be("medium");
    }

    [Fact]
    public void QuestionData_AllTypes_ShouldSupportExplanation()
    {
        // Arrange
        var explanation = "统一的解析内容";

        // Act
        var choice = new ChoiceQuestionData { Explanation = explanation };
        var boolean = new BooleanQuestionData { Explanation = explanation };
        var fillBlank = new FillBlankQuestionData { Explanation = explanation };
        var shortAnswer = new ShortAnswerQuestionData { Explanation = explanation };

        // Assert
        choice.Explanation.Should().Be(explanation);
        boolean.Explanation.Should().Be(explanation);
        fillBlank.Explanation.Should().Be(explanation);
        shortAnswer.Explanation.Should().Be(explanation);
    }

    #endregion
}
