using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;

namespace AnswerMe.Domain.Entities;

/// <summary>
/// 题目实体
/// </summary>
public class Question : BaseEntity
{
    public int QuestionBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// 题型（字符串格式，用于数据库存储和兼容）
    /// 建议：使用 QuestionTypeEnum 属性获取强类型枚举
    /// </summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>
    /// 题型 JSON 数据（新格式，支持多选题等复杂题型）
    /// </summary>
    [Column(TypeName = "json")]
    public string? QuestionDataJson { get; set; }

    /// <summary>
    /// 题型枚举（运行时属性，自动从 QuestionType 字符串解析）
    /// </summary>
    [NotMapped]
    public QuestionType? QuestionTypeEnum
    {
        get => QuestionTypeExtensions.ParseFromString(QuestionType);
        set => QuestionType = value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// 题目数据（运行时属性，自动序列化/反序列化 JSON）
    /// 根据 QuestionTypeEnum 选择正确的数据类型进行反序列化
    /// </summary>
    [NotMapped]
    public QuestionData? Data
    {
        get
        {
            if (string.IsNullOrWhiteSpace(QuestionDataJson))
                return BuildDataFromLegacy();

            try
            {
                // 根据题型选择正确的类型进行反序列化
                return QuestionTypeEnum switch
                {
                    Domain.Enums.QuestionType.SingleChoice => JsonSerializer.Deserialize<ChoiceQuestionData>(QuestionDataJson, QuestionDataJsonOptions.Default),
                    Domain.Enums.QuestionType.MultipleChoice => JsonSerializer.Deserialize<ChoiceQuestionData>(QuestionDataJson, QuestionDataJsonOptions.Default),
                    Domain.Enums.QuestionType.TrueFalse => JsonSerializer.Deserialize<BooleanQuestionData>(QuestionDataJson, QuestionDataJsonOptions.Default),
                    Domain.Enums.QuestionType.FillBlank => JsonSerializer.Deserialize<FillBlankQuestionData>(QuestionDataJson, QuestionDataJsonOptions.Default),
                    Domain.Enums.QuestionType.ShortAnswer => JsonSerializer.Deserialize<ShortAnswerQuestionData>(QuestionDataJson, QuestionDataJsonOptions.Default),
                    _ => null
                };
            }
            catch (JsonException)
            {
                // JSON 反序列化失败，返回 null
                // TODO: 添加日志记录（需要注入 ILogger）
                return BuildDataFromLegacy();
            }
        }
        set => QuestionDataJson = value != null ? JsonSerializer.Serialize(value, QuestionDataJsonOptions.Default) : null;
    }

    /// <summary>
    /// 选项列表（旧格式，已过时，请使用 Data 属性）
    /// </summary>
    [Obsolete("请使用 Data 属性（ChoiceQuestionData.Options）")]
    public string? Options { get; set; }

    /// <summary>
    /// 正确答案（旧格式，已过时，请使用 Data 属性）
    /// </summary>
    [Obsolete("请使用 Data 属性（ChoiceQuestionData.CorrectAnswers）")]
    public string CorrectAnswer { get; set; } = string.Empty;

    /// <summary>
    /// 题目解析
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// 难度级别
    /// </summary>
    public string Difficulty { get; set; } = "medium";

    /// <summary>
    /// 排序索引
    /// </summary>
    public int OrderIndex { get; set; }

    // 导航属性
    public QuestionBank QuestionBank { get; set; } = null!;
    public ICollection<AttemptDetail> AttemptDetails { get; set; } = new List<AttemptDetail>();

    private QuestionData? BuildDataFromLegacy()
    {
        var questionType = QuestionTypeEnum;
        if (questionType == null)
        {
            return null;
        }

        var explanation = Explanation;
        var difficulty = Difficulty;

        switch (questionType)
        {
            case Domain.Enums.QuestionType.SingleChoice:
            case Domain.Enums.QuestionType.MultipleChoice:
            {
                var options = ParseOptions(Options);
                var correctAnswers = ParseAnswers(CorrectAnswer);
                if (options.Count == 0 && correctAnswers.Count == 0 && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new ChoiceQuestionData
                {
                    Options = options,
                    CorrectAnswers = correctAnswers,
                    Explanation = explanation,
                    Difficulty = difficulty
                };
            }
            case Domain.Enums.QuestionType.TrueFalse:
            {
                if (!bool.TryParse(CorrectAnswer, out var booleanAnswer))
                {
                    return null;
                }
                return new BooleanQuestionData
                {
                    CorrectAnswer = booleanAnswer,
                    Explanation = explanation,
                    Difficulty = difficulty
                };
            }
            case Domain.Enums.QuestionType.FillBlank:
            {
                var answers = ParseAnswers(CorrectAnswer);
                if (answers.Count == 0 && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new FillBlankQuestionData
                {
                    AcceptableAnswers = answers,
                    Explanation = explanation,
                    Difficulty = difficulty
                };
            }
            case Domain.Enums.QuestionType.ShortAnswer:
            {
                if (string.IsNullOrWhiteSpace(CorrectAnswer) && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new ShortAnswerQuestionData
                {
                    ReferenceAnswer = CorrectAnswer,
                    Explanation = explanation,
                    Difficulty = difficulty
                };
            }
            default:
                return null;
        }
    }

    private static List<string> ParseOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return new List<string>();
        }

        var trimmed = options.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(options) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static List<string> ParseAnswers(string? answers)
    {
        if (string.IsNullOrWhiteSpace(answers))
        {
            return new List<string>();
        }

        var trimmed = answers.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(answers) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }
}
