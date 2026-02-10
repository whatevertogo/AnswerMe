using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using System.Text.Json.Serialization;

namespace AnswerMe.Application.DTOs;

/// <summary>
/// 创建题目DTO
/// </summary>
public class CreateQuestionDto
{
    public int QuestionBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// 题型（新格式）
    /// </summary>
    public QuestionType? QuestionTypeEnum { get; set; }

    /// <summary>
    /// 题型（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 QuestionTypeEnum")]
    public string? QuestionType
    {
        get => QuestionTypeEnum?.ToString();
        set => QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(value ?? string.Empty);
    }

    /// <summary>
    /// 题目数据（新格式：JSON）
    /// </summary>
    public QuestionData? Data { get; set; }

    /// <summary>
    /// 选项（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.Options")]
    public string? Options { get; set; }

    /// <summary>
    /// 正确答案（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.CorrectAnswers")]
    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
    public int OrderIndex { get; set; }
}

/// <summary>
/// 更新题目DTO
/// </summary>
public class UpdateQuestionDto
{
    public string? QuestionText { get; set; }

    /// <summary>
    /// 题型（新格式）
    /// </summary>
    public QuestionType? QuestionTypeEnum { get; set; }

    /// <summary>
    /// 题型（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete
    /// </summary>
    [Obsolete("请使用 QuestionTypeEnum")]
    public string? QuestionType
    {
        get => QuestionTypeEnum?.ToString();
        set => QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(value ?? string.Empty);
    }

    /// <summary>
    /// 题目数据（新格式：JSON）
    /// </summary>
    public QuestionData? Data { get; set; }

    /// <summary>
    /// 选项（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.Options")]
    public string? Options { get; set; }

    /// <summary>
    /// 正确答案（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.CorrectAnswers")]
    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }
    public string? Difficulty { get; set; }
    public int? OrderIndex { get; set; }
}

/// <summary>
/// 题目响应DTO
/// </summary>
public class QuestionDto
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;

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

    /// <summary>
    /// 题目数据（新格式：JSON，支持多选题）
    /// </summary>
    public QuestionData? Data { get; set; }

    /// <summary>
    /// 选项（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.Options")]
    public string? Options { get; set; }

    /// <summary>
    /// 正确答案（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete，计划 30 天后移除
    /// </summary>
    [Obsolete("请使用 Data.ChoiceQuestionData.CorrectAnswers")]
    public string? CorrectAnswer { get; set; }

    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 向后兼容映射：自动从 Data 填充旧字段
    /// TODO: 移除此方法，由前端直接使用 Data
    /// </summary>
    public void PopulateLegacyFieldsFromData()
    {
        if (Data == null) return;

        Explanation = Data.Explanation ?? Explanation;
        Difficulty = Data.Difficulty ?? Difficulty;

        if (Data is ChoiceQuestionData choiceData)
        {
            Options = choiceData.Options != null ? string.Join(",", choiceData.Options) : null;
            CorrectAnswer = string.Join(",", choiceData.CorrectAnswers);
        }
    }
}

/// <summary>
/// 题目列表查询参数
/// </summary>
public class QuestionListQueryDto
{
    public int QuestionBankId { get; set; }
    public string? Search { get; set; }
    public string? Difficulty { get; set; }

    /// <summary>
    /// 题型过滤（新格式）
    /// </summary>
    public QuestionType? QuestionTypeEnum { get; set; }

    /// <summary>
    /// 题型过滤（旧格式，向后兼容）
    /// TODO: 标记为 Obsolete
    /// </summary>
    [Obsolete("请使用 QuestionTypeEnum")]
    public string? QuestionType
    {
        get => QuestionTypeEnum?.ToString();
        set => QuestionTypeEnum = QuestionTypeExtensions.ParseFromString(value ?? string.Empty);
    }

    public int PageSize { get; set; } = 20;
    public int? LastId { get; set; } // 游标分页
}

/// <summary>
/// 题目列表响应DTO
/// </summary>
public class QuestionListDto
{
    public List<QuestionDto> Data { get; set; } = new();
    public bool HasMore { get; set; }
    public int? NextCursor { get; set; }
    public int TotalCount { get; set; }
}
