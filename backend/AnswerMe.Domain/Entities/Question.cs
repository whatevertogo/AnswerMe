using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Common;

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
            // 如果缓存有效且 JSON 未变化，直接返回缓存
            if (_cachedData != null && QuestionDataJson == _cachedDataJson)
            {
                return _cachedData;
            }

            QuestionData? result;
            if (string.IsNullOrWhiteSpace(QuestionDataJson))
            {
                result = BuildDataFromLegacy();
            }
            else
            {
                try
                {
                    using var jsonDocument = JsonDocument.Parse(QuestionDataJson);
                    var root = jsonDocument.RootElement;

                    var parsed = DeserializeQuestionData();
                    if (parsed == null)
                        result = BuildDataFromLegacy();
                    else
                        result = IsValidQuestionData(root, parsed) ? parsed : BuildDataFromLegacy() ?? parsed;
                }
                catch (JsonException)
                {
                    result = BuildDataFromLegacy();
                }
            }

            // 更新缓存
            _cachedData = result;
            _cachedDataJson = QuestionDataJson;

            return result;
        }
        set
        {
            QuestionDataJson = value != null ? JsonSerializer.Serialize(value, QuestionDataJsonOptions.Default) : null;
            // 更新缓存
            _cachedData = value;
            _cachedDataJson = QuestionDataJson;
        }
    }

    // 缓存字段，避免重复 JSON 解析
    [NonSerialized]
    private QuestionData? _cachedData;
    [NonSerialized]
    private string? _cachedDataJson;

    private QuestionData? DeserializeQuestionData()
    {
        var json = QuestionDataJson!;
        return QuestionTypeEnum switch
        {
            Domain.Enums.QuestionType.SingleChoice => JsonSerializer.Deserialize<ChoiceQuestionData>(json, QuestionDataJsonOptions.Default),
            Domain.Enums.QuestionType.MultipleChoice => JsonSerializer.Deserialize<ChoiceQuestionData>(json, QuestionDataJsonOptions.Default),
            Domain.Enums.QuestionType.TrueFalse => JsonSerializer.Deserialize<BooleanQuestionData>(json, QuestionDataJsonOptions.Default),
            Domain.Enums.QuestionType.FillBlank => JsonSerializer.Deserialize<FillBlankQuestionData>(json, QuestionDataJsonOptions.Default),
            Domain.Enums.QuestionType.ShortAnswer => JsonSerializer.Deserialize<ShortAnswerQuestionData>(json, QuestionDataJsonOptions.Default),
            _ => null
        };
    }

    private static bool IsValidQuestionData(JsonElement root, QuestionData data)
    {
        return data switch
        {
            ChoiceQuestionData cd => root.TryGetProperty("options", out _)
                && root.TryGetProperty("correctAnswers", out _)
                && cd.Options.Count > 0
                && cd.CorrectAnswers.Count > 0,
            BooleanQuestionData => root.TryGetProperty("correctAnswer", out _),
            FillBlankQuestionData fd => root.TryGetProperty("acceptableAnswers", out _)
                && fd.AcceptableAnswers.Count > 0,
            ShortAnswerQuestionData sad => root.TryGetProperty("referenceAnswer", out _)
                && !string.IsNullOrWhiteSpace(sad.ReferenceAnswer),
            _ => true
        };
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "CS0618:旧字段兼容性代码", Justification = "BuildDataFromLegacy 用于从旧字段迁移数据到新格式")]
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                var options = ParseOptions(Options);
                var correctAnswers = ParseAnswers(CorrectAnswer);
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                if (!bool.TryParse(CorrectAnswer, out var booleanAnswer))
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                var answers = ParseAnswers(CorrectAnswer);
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                if (string.IsNullOrWhiteSpace(CorrectAnswer) && string.IsNullOrWhiteSpace(explanation))
#pragma warning restore CS0618
                {
                    return null;
                }
                return new ShortAnswerQuestionData
                {
#pragma warning disable CS0618 // 旧字段兼容性代码
                    ReferenceAnswer = CorrectAnswer,
#pragma warning restore CS0618
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
        return LegacyFieldParser.ParseDelimitedList(options);
    }

    private static List<string> ParseAnswers(string? answers)
    {
        return LegacyFieldParser.ParseCorrectAnswers(answers);
    }
}
