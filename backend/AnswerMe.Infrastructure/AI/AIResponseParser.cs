using System.Text.Json;
using System.Text.RegularExpressions;
using AnswerMe.Application.AI;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 通用AI响应解析器（兼容多种JSON格式与字段名）
/// </summary>
public static class AIResponseParser
{
    public static bool TryParseQuestions(
        string content,
        out List<GeneratedQuestion> questions,
        out string? error)
    {
        questions = new List<GeneratedQuestion>();
        error = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            error = "AI响应为空";
            return false;
        }

        var jsonText = ExtractJsonPayload(content);
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            error = $"未找到有效的JSON内容，响应片段: {BuildSnippet(content)}";
            return false;
        }

        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonText);
            JsonElement questionsElement;

            if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object &&
                TryGetPropertyIgnoreCase(jsonDoc.RootElement, out questionsElement, "questions", "data", "result"))
            {
                // { "questions": [...] }
            }
            else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                questionsElement = jsonDoc.RootElement;
            }
            else
            {
                error = "响应JSON不是题目数组或缺少questions字段";
                return false;
            }

            foreach (var q in questionsElement.EnumerateArray())
            {
                var questionText = GetString(q, "questionText", "question", "content", "title", "题目", "题干", "题目内容");
                var questionTypeText = GetString(q, "questionType", "type", "题型", "类型");
                var explanation = GetString(q, "explanation", "analysis", "解析");
                var difficulty = GetString(q, "difficulty", "难度") ?? "medium";

                var options = GetStringArray(q, "options", "choices", "选项");
                var correctAnswer = GetString(q, "correctAnswer", "answer", "correct", "正确答案", "答案");

                // 解析题型
                QuestionType? questionTypeEnum = null;
                if (!string.IsNullOrWhiteSpace(questionTypeText))
                {
                    questionTypeEnum = QuestionTypeExtensions.ParseFromString(questionTypeText);
                }

                // 构建题目数据（新格式）
                QuestionData? data = null;
                if (options.Count > 0)
                {
                    // 选择题
                    var correctAnswers = ParseCorrectAnswers(correctAnswer);
                    data = new ChoiceQuestionData
                    {
                        Options = options,
                        CorrectAnswers = correctAnswers,
                        Explanation = explanation,
                        Difficulty = difficulty
                    };
                }
                else if (questionTypeEnum == QuestionType.TrueFalse)
                {
                    // 判断题
                    if (bool.TryParse(correctAnswer, out var boolAnswer))
                    {
                        data = new BooleanQuestionData
                        {
                            CorrectAnswer = boolAnswer,
                            Explanation = explanation,
                            Difficulty = difficulty
                        };
                    }
                }
                else if (questionTypeEnum == QuestionType.FillBlank)
                {
                    // 填空题
                    data = new FillBlankQuestionData
                    {
                        AcceptableAnswers = ParseCorrectAnswers(correctAnswer),
                        Explanation = explanation,
                        Difficulty = difficulty
                    };
                }
                else if (questionTypeEnum == QuestionType.ShortAnswer)
                {
                    // 简答题
                    data = new ShortAnswerQuestionData
                    {
                        ReferenceAnswer = correctAnswer ?? string.Empty,
                        Explanation = explanation,
                        Difficulty = difficulty
                    };
                }

                var generated = new GeneratedQuestion
                {
                    QuestionText = questionText ?? string.Empty,
                    Explanation = explanation,
                    Difficulty = difficulty,
                    Data = data,
#pragma warning disable CS0618 // 旧字段兼容性代码：填充旧字段供后续代码使用
                    Options = options,
                    CorrectAnswer = correctAnswer ?? string.Empty
#pragma warning restore CS0618
                };

                generated.QuestionTypeEnum = questionTypeEnum;

                questions.Add(generated);
            }

            if (questions.Count == 0)
            {
                error = $"解析到的题目为空，响应片段: {BuildSnippet(jsonText)}";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"JSON解析失败: {ex.Message}，响应片段: {BuildSnippet(jsonText)}";
            return false;
        }
        catch (Exception ex)
        {
            error = $"解析失败: {ex.Message}";
            return false;
        }
    }

    private static List<string> ParseCorrectAnswers(string? correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(correctAnswer))
        {
            return new List<string>();
        }

        // 尝试解析 JSON 数组
        if (correctAnswer.TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(correctAnswer) ?? new List<string>();
            }
            catch
            {
                // JSON 解析失败，继续尝试其他方式
            }
        }

        // 按分隔符分割
        return correctAnswer
            .Split(new[] { ',', ';', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetPropertyIgnoreCase(element, out var prop, name))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
                if (prop.ValueKind != JsonValueKind.Null && prop.ValueKind != JsonValueKind.Undefined)
                {
                    return prop.ToString();
                }
            }
        }

        return null;
    }

    private static List<string> GetStringArray(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetPropertyIgnoreCase(element, out var prop, name))
            {
                continue;
            }

            if (prop.ValueKind == JsonValueKind.Array)
            {
                return prop.EnumerateArray()
                    .Select(item =>
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            return item.GetString() ?? string.Empty;
                        }
                        if (item.ValueKind == JsonValueKind.Object &&
                            item.TryGetProperty("text", out var textProp) &&
                            textProp.ValueKind == JsonValueKind.String)
                        {
                            return textProp.GetString() ?? string.Empty;
                        }
                        return item.ToString();
                    })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();
            }

            if (prop.ValueKind == JsonValueKind.Object)
            {
                var items = prop.EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(p =>
                    {
                        if (p.Value.ValueKind == JsonValueKind.String)
                        {
                            return p.Value.GetString() ?? string.Empty;
                        }
                        return p.Value.ToString();
                    })
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

                if (items.Count > 0)
                {
                    return items;
                }
            }

            if (prop.ValueKind == JsonValueKind.String)
            {
                var raw = prop.GetString() ?? string.Empty;
                var parts = raw
                    .Split(new[] { '\n', ';', '；', '|', '｜' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                if (parts.Count > 0)
                {
                    return parts;
                }
            }
        }

        return new List<string>();
    }

    private static bool TryGetPropertyIgnoreCase(
        JsonElement element,
        out JsonElement property,
        params string[] names)
    {
        foreach (var name in names)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    property = prop.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static string ExtractJsonPayload(string content)
    {
        // 优先从代码块中提取 JSON
        var fenceMatch = Regex.Match(
            content,
            "```(?:json)?\\s*([\\s\\S]*?)\\s*```",
            RegexOptions.IgnoreCase);
        if (fenceMatch.Success)
        {
            content = fenceMatch.Groups[1].Value;
        }

        var trimmed = content.Trim();
        var objStart = trimmed.IndexOf('{');
        var objEnd = trimmed.LastIndexOf('}');
        var arrStart = trimmed.IndexOf('[');
        var arrEnd = trimmed.LastIndexOf(']');

        if (arrStart >= 0 && arrEnd > arrStart && (objStart < 0 || arrStart < objStart))
        {
            return trimmed.Substring(arrStart, arrEnd - arrStart + 1);
        }

        if (objStart >= 0 && objEnd > objStart)
        {
            return trimmed.Substring(objStart, objEnd - objStart + 1);
        }

        return string.Empty;
    }

    private static string BuildSnippet(string content, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var normalized = content.Replace("\r", " ").Replace("\n", " ").Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized.Substring(0, maxLength) + "...";
    }
}
