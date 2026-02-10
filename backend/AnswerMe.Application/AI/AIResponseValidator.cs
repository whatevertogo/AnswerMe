using System.Text.Json;
using AnswerMe.Application.AI;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;

namespace AnswerMe.Application.AI;

/// <summary>
/// AI响应验证器
/// </summary>
public class AIResponseValidator
{
    /// <summary>
    /// 验证AI生成的题目响应
    /// </summary>
    public static (bool IsValid, List<string> Errors) ValidateQuestionsResponse(string jsonResponse)
    {
        var errors = new List<string>();

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonResponse);

            if (!jsonDoc.RootElement.TryGetProperty("questions", out var questionsElement))
            {
                errors.Add("响应缺少questions字段");
                return (false, errors);
            }

            var questions = questionsElement.EnumerateArray().ToList();
            if (questions.Count == 0)
            {
                errors.Add("questions数组为空");
                return (false, errors);
            }

            foreach (var q in questions)
            {
                // 验证必需字段
                if (!q.TryGetProperty("questionType", out var questionType) || string.IsNullOrEmpty(questionType.GetString()))
                    errors.Add("题目缺少questionType字段");

                if (!q.TryGetProperty("questionText", out var questionText) || string.IsNullOrEmpty(questionText.GetString()))
                    errors.Add("题目缺少questionText字段");

                if (!q.TryGetProperty("options", out var options))
                    errors.Add("题目缺少options字段");
                else
                {
                    var optionsList = options.EnumerateArray().ToList();
                    if (optionsList.Count < 2)
                        errors.Add("题目选项数量不能少于2个");

                    // 检查选项是否唯一
                    var optionValues = optionsList.Select(o => o.GetString()).ToList();
                    if (optionValues.Distinct().Count() != optionValues.Count)
                        errors.Add("题目选项不能重复");
                }

                if (!q.TryGetProperty("correctAnswer", out var correctAnswer) || string.IsNullOrEmpty(correctAnswer.GetString()))
                    errors.Add("题目缺少correctAnswer字段");

                if (!q.TryGetProperty("difficulty", out var difficulty))
                    errors.Add("题目缺少difficulty字段");

                // 验证difficulty值
                if (difficulty.ValueKind != JsonValueKind.Undefined)
                {
                    var validDifficulties = new[] { "easy", "medium", "hard" };
                    var diffValue = difficulty.GetString();
                    if (!validDifficulties.Contains(diffValue))
                        errors.Add($"难度值'{diffValue}'无效，必须是easy/medium/hard之一");
                }
            }

            return (errors.Count == 0, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON格式错误: {ex.Message}");
            return (false, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"验证失败: {ex.Message}");
            return (false, errors);
        }
    }

    /// <summary>
    /// 验证并解析AI响应
    /// </summary>
    public static (AIQuestionGenerateResponse? Response, List<string> Errors) ValidateAndParse(string jsonResponse)
    {
        var (isValid, errors) = ValidateQuestionsResponse(jsonResponse);

        if (!isValid)
        {
            return (null, errors);
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonResponse);
            var questions = new List<GeneratedQuestion>();

            foreach (var q in jsonDoc.RootElement.GetProperty("questions").EnumerateArray())
            {
                var questionTypeStr = q.GetProperty("questionType").GetString() ?? "";
                var questionTypeEnum = QuestionTypeExtensions.ParseFromString(questionTypeStr);
                var questionText = q.GetProperty("questionText").GetString() ?? "";
                var explanation = q.GetProperty("explanation").GetString() ?? "";
                var difficulty = q.GetProperty("difficulty").GetString() ?? "medium";

                var options = q.GetProperty("options").EnumerateArray()
                    .Select(x => x.GetString() ?? "")
                    .ToList();
                var correctAnswer = q.GetProperty("correctAnswer").GetString() ?? "";

                // 构建新格式的 Data
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

#pragma warning disable CS0618 // 旧字段兼容性代码
                questions.Add(new GeneratedQuestion
                {
                    QuestionTypeEnum = questionTypeEnum,
                    QuestionText = questionText,
                    Data = data,
                    Options = options,
                    CorrectAnswer = correctAnswer,
                    Explanation = explanation,
                    Difficulty = difficulty
                });
#pragma warning restore CS0618
            }

            return (new AIQuestionGenerateResponse
            {
                Success = true,
                Questions = questions
            }, new List<string>());
        }
        catch (Exception ex)
        {
            errors.Add($"解析失败: {ex.Message}");
            return (null, errors);
        }
    }

    private static List<string> ParseCorrectAnswers(string correctAnswer)
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
            .Split(new[] { ',', ';', '、' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }
}
