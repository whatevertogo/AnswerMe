using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using System.Text.Json.Serialization;

namespace AnswerMe.Application.DTOs;

/// <summary>
/// AI生成题目请求DTO
/// </summary>
public class AIGenerateRequestDto
{
    /// <summary>
    /// 题库ID（可选，如果提供则将生成的题目添加到该题库）
    /// </summary>
    public int? QuestionBankId { get; set; }

    /// <summary>
    /// 生成主题
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// 生成数量
    /// </summary>
    public int Count { get; set; } = 10;

    /// <summary>
    /// 难度等级：easy, medium, hard
    /// </summary>
    public string Difficulty { get; set; } = "medium";

    /// <summary>
    /// 题型列表（新格式：使用枚举）
    /// </summary>
    public List<QuestionType> QuestionTypes { get; set; } = new();

    /// <summary>
    /// 题型列表（旧格式：字符串，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 QuestionTypes（枚举格式）")]
    [JsonIgnore]
    public List<string>? QuestionTypesLegacy
    {
        get => QuestionTypes.Select(qt => qt.ToString()).ToList();
        set => QuestionTypes = value?.Select(v =>
        {
            // 尝试解析字符串为枚举
            if (Enum.TryParse<QuestionType>(v, ignoreCase: true, out var result))
                return result;
            // 兼容旧格式
            return v.ToLowerInvariant() switch
            {
                "choice" or "single" => QuestionType.SingleChoice,
                "multiple" => QuestionType.MultipleChoice,
                "true-false" or "boolean" => QuestionType.TrueFalse,
                "fill" => QuestionType.FillBlank,
                "essay" => QuestionType.ShortAnswer,
                _ => QuestionType.SingleChoice
            };
        }).ToList() ?? new List<QuestionType>();
    }

    /// <summary>
    /// 语言：zh-CN, en-US
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// 自定义提示词（可选）
    /// </summary>
    public string? CustomPrompt { get; set; }

    /// <summary>
    /// AI Provider名称（OpenAI、Qwen等，默认使用用户配置的第一个可用Provider）
    /// </summary>
    public string? ProviderName { get; set; }
}

/// <summary>
/// AI生成题目响应DTO
/// </summary>
public class AIGenerateResponseDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 生成的题目列表
    /// </summary>
    public List<GeneratedQuestionDto> Questions { get; set; } = new();

    /// <summary>
    /// 任务ID（用于异步任务查询）
    /// </summary>
    public string? TaskId { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 使用的Token数量
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// 部分成功：返回成功生成的题目数量
    /// </summary>
    public int? PartialSuccessCount { get; set; }
}

/// <summary>
/// 生成的题目DTO
/// </summary>
public class GeneratedQuestionDto
{
    public int Id { get; set; }

    /// <summary>
    /// 题型（新格式：枚举）
    /// </summary>
    public QuestionType? QuestionTypeEnum { get; set; }

    /// <summary>
    /// 题型（旧格式：字符串，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 QuestionTypeEnum")]
    public string QuestionType
    {
        get => QuestionTypeEnum?.ToString() ?? string.Empty;
        set => QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(value);
    }

    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// 题目数据（新格式：JSON，支持多选题等）
    /// </summary>
    public QuestionData? Data { get; set; }

    /// <summary>
    /// 选项列表（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.Options")]
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// 正确答案（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.CorrectAnswers")]
    public string CorrectAnswer { get; set; } = string.Empty;

    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
    public int QuestionBankId { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 向后兼容映射：自动从 Data 填充旧字段
    /// TODO: 移除此方法，由前端直接使用 Data
    /// </summary>
    public void PopulateLegacyFieldsFromData()
    {
        if (Data == null) return;

        Explanation = Data.Explanation ?? Explanation;
        Difficulty = Data.Difficulty ?? Difficulty;

#pragma warning disable CS0618 // 旧字段兼容性代码
        if (Data is ChoiceQuestionData choiceData)
        {
            Options = choiceData.Options;
            CorrectAnswer = string.Join(",", choiceData.CorrectAnswers);
        }
#pragma warning restore CS0618
    }
}

/// <summary>
/// 生成进度查询响应DTO
/// </summary>
public class AIGenerateProgressDto
{
    /// <summary>
    /// 任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// 用户ID（用于权限验证）
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 任务状态：pending, processing, completed, failed, partial_success
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// 已生成的题目数量
    /// </summary>
    public int GeneratedCount { get; set; }

    /// <summary>
    /// 总题目数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 错误消息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 已生成的题目（部分成功时返回）
    /// </summary>
    public List<GeneratedQuestionDto>? Questions { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}
