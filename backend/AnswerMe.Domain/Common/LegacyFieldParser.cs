using System.Text.Json;

namespace AnswerMe.Domain.Common;

/// <summary>
/// 旧字段解析工具类（用于向后兼容）
/// 统一处理 Options、CorrectAnswer 等旧字段的解析逻辑
/// </summary>
public static class LegacyFieldParser
{
    private static readonly string[] TrueValues = ["true", "t", "1", "yes", "y", "on", "对", "正确", "是"];
    private static readonly string[] FalseValues = ["false", "f", "0", "no", "n", "off", "错", "错误", "否"];

    /// <summary>
    /// 解析分隔符分隔的字符串或 JSON 数组
    /// </summary>
    public static List<string> ParseDelimitedList(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string>();
        }

        var trimmed = input.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(input) ?? new List<string>();
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

    /// <summary>
    /// 解析支持多种分隔符的正确答案（逗号、分号、顿号等）
    /// </summary>
    public static List<string> ParseCorrectAnswers(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string>();
        }

        // 尝试解析 JSON 数组
        if (input.TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(input) ?? new List<string>();
            }
            catch
            {
                // JSON 解析失败，继续尝试其他方式
            }
        }

        // 按分隔符分割
        return input
            .Split(new[] { ',', ';', '、' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    /// <summary>
    /// 解析布尔答案（兼容历史格式：true/false、1/0、对/错、正确/错误、yes/no）
    /// </summary>
    public static bool? ParseBooleanAnswer(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var normalized = input.Trim().ToLowerInvariant();

        if (TrueValues.Contains(normalized))
        {
            return true;
        }

        if (FalseValues.Contains(normalized))
        {
            return false;
        }

        return null;
    }
}
