namespace AnswerMe.Domain.Enums;

/// <summary>
/// 题目类型枚举
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 单选题 - 只有一个正确答案
    /// </summary>
    SingleChoice,

    /// <summary>
    /// 多选题 - 有 2-3 个正确答案
    /// </summary>
    MultipleChoice,

    /// <summary>
    /// 判断题 - 答案为 true 或 false
    /// </summary>
    TrueFalse,

    /// <summary>
    /// 填空题 - 需要填写答案
    /// </summary>
    FillBlank,

    /// <summary>
    /// 简答题 - 需要文字回答
    /// </summary>
    ShortAnswer
}

/// <summary>
/// QuestionType 枚举扩展方法
/// </summary>
public static class QuestionTypeExtensions
{
    /// <summary>
    /// 获取题型的显示名称（中文）
    /// </summary>
    public static string DisplayName(this QuestionType type) =>
        type switch
        {
            QuestionType.SingleChoice => "单选题",
            QuestionType.MultipleChoice => "多选题",
            QuestionType.TrueFalse => "判断题",
            QuestionType.FillBlank => "填空题",
            QuestionType.ShortAnswer => "简答题",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"未知的题型: {type}")
        };

    /// <summary>
    /// 获取题型在 AI Prompt 中的格式
    /// </summary>
    public static string ToAiPrompt(this QuestionType type) =>
        type switch
        {
            QuestionType.SingleChoice => "single_choice",
            QuestionType.MultipleChoice => "multiple_choice",
            QuestionType.TrueFalse => "true_false",
            QuestionType.FillBlank => "fill_blank",
            QuestionType.ShortAnswer => "short_answer",
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"未知的题型: {type}")
        };

    /// <summary>
    /// 从字符串解析 QuestionType（兼容旧格式）
    /// </summary>
    public static QuestionType? ParseFromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // 标准枚举名称
        if (Enum.TryParse<QuestionType>(value, ignoreCase: true, out var result))
            return result;

        // 旧格式映射
        return value.ToLowerInvariant() switch
        {
            "choice" or "single" or "single-choice" or "单选题" or "单选" or "选择题" => QuestionType.SingleChoice,
            "multiple" or "multiple-choice" or "多选题" => QuestionType.MultipleChoice,
            "true-false" or "boolean" or "bool" or "判断题" => QuestionType.TrueFalse,
            "fill" or "fill-blank" or "填空题" => QuestionType.FillBlank,
            "essay" or "short-answer" or "简答题" => QuestionType.ShortAnswer,
            _ => null
        };
    }
}
