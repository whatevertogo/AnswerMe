using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;

namespace AnswerMe.Application.AI;

/// <summary>
/// AI题目生成请求
/// </summary>
public class AIQuestionGenerateRequest
{
    public string Subject { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Difficulty { get; set; } = "medium";

    /// <summary>
    /// 题型列表（新格式：枚举）
    /// </summary>
    public List<QuestionType> QuestionTypes { get; set; } = new();

    public string? CustomPrompt { get; set; }
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// AI题目生成响应
/// </summary>
public class AIQuestionGenerateResponse
{
    public bool Success { get; set; }
    public List<GeneratedQuestion> Questions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int? TokensUsed { get; set; }
}

/// <summary>
/// 生成的题目
/// </summary>
public class GeneratedQuestion
{
    /// <summary>
    /// 题型（新格式：枚举）
    /// </summary>
    public QuestionType? QuestionTypeEnum { get; set; }

    /// <summary>
    /// 题型（旧格式：字符串，用于向后兼容）
    /// TODO: 标记为 Obsolete，计划移除
    /// </summary>
    [Obsolete("请使用 QuestionTypeEnum")]
    public string QuestionType
    {
        get => QuestionTypeEnum?.ToString() ?? string.Empty;
        set => QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(value);
    }

    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// 题目数据（新格式：JSON，支持多选题）
    /// </summary>
    public QuestionData? Data { get; set; }

    /// <summary>
    /// 选项列表（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.Options")]
    public List<string> Options { get; set; } = new();

    /// <summary>
    /// 正确答案（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.CorrectAnswers")]
    public string CorrectAnswer { get; set; } = string.Empty;

    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
}
