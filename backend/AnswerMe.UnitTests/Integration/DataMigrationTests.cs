using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Xunit;
using System.Net.Http.Json;
using System.Text.Json;

namespace AnswerMe.UnitTests.Integration;

/// <summary>
/// 数据迁移验证测试
/// 验证从旧格式到新格式的数据迁移
/// </summary>
public class DataMigrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public DataMigrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migration_ShouldPreserveAllOldFormatData()
    {
        // Arrange - 创建 100 条旧格式样本数据
        var oldQuestions = TestDataGeneratorExtensions.GenerateOldFormatQuestions(100);

        // Act - 模拟迁移过程
        var migratedQuestions = new List<Question>();
        foreach (var oldQuestion in oldQuestions)
        {
            var migrated = MigrateOldToNew(oldQuestion);
            migratedQuestions.Add(migrated);
        }

        // Assert
        migratedQuestions.Should().HaveCount(100, "应该迁移所有 100 条数据");

        // 验证选择题迁移正确
        var choiceQuestions = migratedQuestions
            .Where(q => q.Data is ChoiceQuestionData)
            .ToList();
        choiceQuestions.Count.Should().BeGreaterThanOrEqualTo(60, "至少 60% 应该是选择题");

        // 验证判断题迁移正确
        var booleanQuestions = migratedQuestions
            .Where(q => q.Data is BooleanQuestionData)
            .ToList();
        booleanQuestions.Count.Should().BeGreaterThanOrEqualTo(10, "至少 10% 应该是判断题");

        // 验证填空题迁移正确
        var fillBlankQuestions = migratedQuestions
            .Where(q => q.Data is FillBlankQuestionData)
            .ToList();
        fillBlankQuestions.Count.Should().BeGreaterThanOrEqualTo(10, "至少 10% 应该是填空题");

        // 验证简答题迁移正确
        var shortAnswerQuestions = migratedQuestions
            .Where(q => q.Data is ShortAnswerQuestionData)
            .ToList();
        shortAnswerQuestions.Count.Should().BeGreaterThanOrEqualTo(10, "至少 10% 应该是简答题");
    }

    [Fact]
    public async Task Migration_ChoiceQuestion_ShouldMapOptionsCorrectly()
    {
        // Arrange - 旧格式单选题
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "JavaScript 是什么?",
            QuestionType = "choice",
            Options = "[\"A. 编程语言\", \"B. 标记语言\", \"C. 样式语言\", \"D. 数据库语言\"]",
            CorrectAnswer = "A",
            Explanation = "JavaScript 是一种编程语言",
            Difficulty = "easy"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert
        migrated.Data.Should().BeOfType<ChoiceQuestionData>();
        var choiceData = migrated.Data as ChoiceQuestionData;
        choiceData!.Options.Should().BeEquivalentTo(new[]
        {
            "A. 编程语言",
            "B. 标记语言",
            "C. 样式语言",
            "D. 数据库语言"
        });
        choiceData.CorrectAnswers.Should().ContainSingle("A");
        choiceData.Explanation.Should().Be("JavaScript 是一种编程语言");
        choiceData.Difficulty.Should().Be("easy");
    }

    [Fact]
    public async Task Migration_MultipleChoice_ShouldMapToMultipleCorrectAnswers()
    {
        // Arrange - 旧格式多选题（模拟，使用逗号分隔）
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "哪些是 Web 前端技术?",
            QuestionType = "multiple",
            Options = "[\"A. HTML\", \"B. CSS\", \"C. JavaScript\", \"D. Python\"]",
            CorrectAnswer = "A,B,C", // 模拟旧格式可能存储多答案
            Explanation = "HTML、CSS、JavaScript 都是前端技术",
            Difficulty = "medium"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert
        migrated.QuestionTypeEnum.Should().Be(QuestionType.MultipleChoice);
        var choiceData = migrated.Data as ChoiceQuestionData;
        choiceData!.CorrectAnswers.Should().BeEquivalentTo(new[] { "A", "B", "C" });
    }

    [Fact]
    public async Task Migration_BooleanQuestion_ShouldMapCorrectly()
    {
        // Arrange - 旧格式判断题
        var testCases = new[]
        {
            ("true", true),
            ("false", false),
            ("True", true),
            ("False", false),
            ("对", true),
            ("错", false),
            ("T", true),
            ("F", false),
            ("yes", true),
            ("no", false)
        };

        foreach (var (oldAnswer, expected) in testCases)
        {
            // Arrange
            var oldQuestion = new Question
            {
                QuestionBankId = 1,
                QuestionText = "地球是圆的?",
                QuestionType = "true-false",
                CorrectAnswer = oldAnswer,
                Explanation = "地理知识",
                Difficulty = "easy"
            };

            // Act
            var migrated = MigrateOldToNew(oldQuestion);

            // Assert
            migrated.Data.Should().BeOfType<BooleanQuestionData>();
            var booleanData = migrated.Data as BooleanQuestionData;
            booleanData!.CorrectAnswer.Should().Be(expected);
        }
    }

    [Fact]
    public async Task Migration_FillBlankQuestion_ShouldMapCorrectly()
    {
        // Arrange - 旧格式填空题
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "中国的首都是____",
            QuestionType = "fill",
            CorrectAnswer = "北京",
            Explanation = "地理知识",
            Difficulty = "medium"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert
        migrated.Data.Should().BeOfType<FillBlankQuestionData>();
        var fillData = migrated.Data as FillBlankQuestionData;
        fillData!.AcceptableAnswers.Should().ContainSingle();
        fillData.AcceptableAnswers[0].Should().Be("北京");
    }

    [Fact]
    public async Task Migration_ShortAnswerQuestion_ShouldMapCorrectly()
    {
        // Arrange - 旧格式简答题
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "请简述 HTTP 协议的作用",
            QuestionType = "essay",
            CorrectAnswer = "HTTP 是超文本传输协议，用于 Web 通信",
            Explanation = "网络基础知识",
            Difficulty = "hard"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert
        migrated.Data.Should().BeOfType<ShortAnswerQuestionData>();
        var shortData = migrated.Data as ShortAnswerQuestionData;
        shortData!.ReferenceAnswer.Should().Be("HTTP 是超文本传输协议，用于 Web 通信");
    }

    [Fact]
    public async Task Migration_ShouldPreserveExplanationAndDifficulty()
    {
        // Arrange
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\"]",
            CorrectAnswer = "A",
            Explanation = "这是重要的解析内容",
            Difficulty = "hard"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert
        migrated.Data.Should().NotBeNull();
        migrated.Data!.Explanation.Should().Be("这是重要的解析内容");
        migrated.Data.Difficulty.Should().Be("hard");
    }

    [Fact]
    public async Task Migration_ShouldUpdateQuestionTypeToEnum()
    {
        // Arrange & Act
        var testCases = new[]
        {
            ("choice", QuestionType.SingleChoice),
            ("single", QuestionType.SingleChoice),
            ("single-choice", QuestionType.SingleChoice),
            ("multiple", QuestionType.MultipleChoice),
            ("multiple-choice", QuestionType.MultipleChoice),
            ("true-false", QuestionType.TrueFalse),
            ("boolean", QuestionType.TrueFalse),
            ("bool", QuestionType.TrueFalse),
            ("fill", QuestionType.FillBlank),
            ("fill-blank", QuestionType.FillBlank),
            ("essay", QuestionType.ShortAnswer),
            ("short-answer", QuestionType.ShortAnswer)
        };

        foreach (var (oldType, expectedEnum) in testCases)
        {
            // Arrange
            var oldQuestion = new Question
            {
                QuestionBankId = 1,
                QuestionText = "测试",
                QuestionType = oldType,
                Options = "[\"A. Test\"]",
                CorrectAnswer = "A"
            };

            // Act
            var migrated = MigrateOldToNew(oldQuestion);

            // Assert
            migrated.QuestionTypeEnum.Should().Be(expectedEnum,
                $"旧题型 '{oldType}' 应该迁移到 {expectedEnum}");
        }
    }

    [Fact]
    public async Task Migration_ShouldHandleNullOptionsAndExplanation()
    {
        // Arrange - 边界情况: null 值
        var oldQuestion = new Question
        {
            QuestionBankId = 1,
            QuestionText = "测试题目",
            QuestionType = "choice",
            Options = null,  // 没有 options
            CorrectAnswer = "",
            Explanation = null,  // 没有 explanation
            Difficulty = "medium"
        };

        // Act
        var migrated = MigrateOldToNew(oldQuestion);

        // Assert - 应该优雅处理 null 值
        migrated.Data.Should().NotBeNull();
        migrated.Data!.Explanation.Should().BeNull();
        migrated.Data.Difficulty.Should().Be("medium");
    }

    [Fact]
    public async Task Migration_RoundTrip_ShouldNotLoseData()
    {
        // Arrange - 创建旧格式题目
        var original = new Question
        {
            QuestionBankId = 1,
            QuestionText = "往返测试",
            QuestionType = "choice",
            Options = "[\"A. 选项1\", \"B. 选项2\", \"C. 选项3\"]",
            CorrectAnswer = "B",
            Explanation = "完整解析",
            Difficulty = "medium"
        };

        // Act - 迁移到新格式
        var migrated = MigrateOldToNew(original);

        // Assert - 验证数据完整性
        migrated.QuestionText.Should().Be(original.QuestionText);

        var choiceData = migrated.Data as ChoiceQuestionData;
        choiceData.Should().NotBeNull();
        choiceData!.Options.Should().HaveCount(3);
        choiceData.CorrectAnswers.Should().ContainSingle("B");
        choiceData.Explanation.Should().Be("完整解析");
        choiceData.Difficulty.Should().Be("medium");
    }

    /// <summary>
    /// 模拟数据迁移逻辑（从旧格式转换为新格式）
    /// 这是迁移脚本应该在数据库中执行的操作的 C# 版本
    /// </summary>
    private Question MigrateOldToNew(Question oldQuestion)
    {
        var migrated = new Question
        {
            Id = oldQuestion.Id,
            QuestionBankId = oldQuestion.QuestionBankId,
            QuestionText = oldQuestion.QuestionText,
            OrderIndex = oldQuestion.OrderIndex,
            CreatedAt = oldQuestion.CreatedAt,
            UpdatedAt = oldQuestion.UpdatedAt
        };

        // 1. 迁移题型为枚举
        migrated.QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(oldQuestion.QuestionType);

        // 2. 根据旧题型转换为新格式数据
        switch (oldQuestion.QuestionType?.ToLowerInvariant())
        {
            case "choice":
            case "single":
            case "single-choice":
                // 选择题
                migrated.QuestionTypeEnum = QuestionType.SingleChoice;
                var options = oldQuestion.Options != null
                    ? JsonSerializer.Deserialize<List<string>>(oldQuestion.Options)
                    : new List<string>();
                migrated.Data = new ChoiceQuestionData
                {
                    Options = options,
                    CorrectAnswers = new List<string> { oldQuestion.CorrectAnswer },
                    Explanation = oldQuestion.Explanation,
                    Difficulty = oldQuestion.Difficulty
                };
                break;

            case "multiple":
            case "multiple-choice":
                // 多选题 (假设旧格式用逗号分隔)
                migrated.QuestionTypeEnum = QuestionType.MultipleChoice;
                var multiOptions = oldQuestion.Options != null
                    ? JsonSerializer.Deserialize<List<string>>(oldQuestion.Options)
                    : new List<string>();
                var multiAnswers = oldQuestion.CorrectAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(a => a.Trim())
                    .ToList();
                migrated.Data = new ChoiceQuestionData
                {
                    Options = multiOptions,
                    CorrectAnswers = multiAnswers,
                    Explanation = oldQuestion.Explanation,
                    Difficulty = oldQuestion.Difficulty
                };
                break;

            case "true-false":
            case "boolean":
            case "bool":
                // 判断题
                migrated.QuestionTypeEnum = QuestionType.TrueFalse;
                var boolValue = oldQuestion.CorrectAnswer.ToLowerInvariant() switch
                {
                    "true" or "t" or "对" or "正确" or "yes" or "y" => true,
                    _ => false
                };
                migrated.Data = new BooleanQuestionData
                {
                    CorrectAnswer = boolValue,
                    Explanation = oldQuestion.Explanation,
                    Difficulty = oldQuestion.Difficulty
                };
                break;

            case "fill":
            case "fill-blank":
            case "fill_blank":
                // 填空题
                migrated.QuestionTypeEnum = QuestionType.FillBlank;
                migrated.Data = new FillBlankQuestionData
                {
                    AcceptableAnswers = new List<string> { oldQuestion.CorrectAnswer },
                    Explanation = oldQuestion.Explanation,
                    Difficulty = oldQuestion.Difficulty
                };
                break;

            case "essay":
            case "short-answer":
            case "short_answer":
                // 简答题
                migrated.QuestionTypeEnum = QuestionType.ShortAnswer;
                migrated.Data = new ShortAnswerQuestionData
                {
                    ReferenceAnswer = oldQuestion.CorrectAnswer,
                    Explanation = oldQuestion.Explanation,
                    Difficulty = oldQuestion.Difficulty
                };
                break;

            default:
                // 未知类型,默认为单选
                migrated.QuestionTypeEnum = QuestionType.SingleChoice;
                if (oldQuestion.Options != null)
                {
                    var defOptions = JsonSerializer.Deserialize<List<string>>(oldQuestion.Options);
                    migrated.Data = new ChoiceQuestionData
                    {
                        Options = defOptions ?? new List<string>(),
                        CorrectAnswers = new List<string> { oldQuestion.CorrectAnswer },
                        Explanation = oldQuestion.Explanation,
                        Difficulty = oldQuestion.Difficulty
                    };
                }
                break;
        }

        return migrated;
    }
}

/// <summary>
/// 测试数据生成器扩展（添加到现有 Helpers/TestDataGenerator.cs）
/// </summary>
public static class TestDataGeneratorExtensions
{
    private static readonly Random _random = new(42);

    public static List<Question> GenerateOldFormatQuestions(int count)
    {
        var questions = new List<Question>();
        var types = new[] { "choice", "multiple", "true-false", "fill", "essay" };
        var difficulties = new[] { "easy", "medium", "hard" };

        for (int i = 0; i < count; i++)
        {
            var type = types[_random.Next(types.Length)];
            var difficulty = difficulties[_random.Next(difficulties.Length)];

            var question = new Question
            {
                Id = i + 1,
                QuestionBankId = 1,
                QuestionText = $"测试题目 {i + 1}",
                QuestionType = type,
                Options = type is "choice" or "single" or "multiple"
                    ? JsonSerializer.Serialize(new[] { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" })
                    : null,
                CorrectAnswer = GetRandomCorrectAnswer(type),
                Explanation = $"测试解析 {i + 1}",
                Difficulty = difficulty,
                OrderIndex = i,
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365)),
                UpdatedAt = DateTime.UtcNow
            };

            questions.Add(question);
        }

        return questions;
    }

    private static string GetRandomCorrectAnswer(string type)
    {
        return type switch
        {
            "choice" or "single" => "A",
            "multiple" => "A,B",
            "true-false" => "true",
            "fill" => "答案",
            "essay" => "参考答案",
            _ => "A"
        };
    }
}
