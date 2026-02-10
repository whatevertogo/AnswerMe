using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnswerMe.Infrastructure.Services;

/// <summary>
/// 数据一致性检查服务
/// 检查 QuestionDataJson 与 QuestionType 的一致性
/// </summary>
public class DataConsistencyCheckService
{
    private readonly AnswerMeDbContext _context;
    private readonly ILogger<DataConsistencyCheckService> _logger;

    public DataConsistencyCheckService(
        AnswerMeDbContext context,
        ILogger<DataConsistencyCheckService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 检查所有题目的数据一致性
    /// </summary>
    public async Task<DataConsistencyReport> CheckAllQuestionsAsync(CancellationToken cancellationToken = default)
    {
        var report = new DataConsistencyReport();

#pragma warning disable CS0618 // 旧字段兼容性代码：检查数据一致性时读取旧字段
        var questions = await _context.Questions
            .Select(q => new
            {
                q.Id,
                q.QuestionType,
                q.QuestionDataJson,
                q.Options,
                q.CorrectAnswer
            })
            .ToListAsync(cancellationToken);
#pragma warning restore CS0618

        report.TotalQuestions = questions.Count;

        foreach (var question in questions)
        {
            var issue = CheckQuestionConsistency(question);
            if (issue != null)
            {
                report.Issues.Add(issue);
            }
        }

        report.InconsistentQuestions = report.Issues.Count;
        report.ConsistencyRate = report.TotalQuestions > 0
            ? (double)(report.TotalQuestions - report.InconsistentQuestions) / report.TotalQuestions * 100
            : 100;

        return report;
    }

    /// <summary>
    /// 检查单个题目的数据一致性
    /// </summary>
    private DataConsistencyIssue? CheckQuestionConsistency(dynamic question)
    {
        var questionType = question.QuestionType as string;
        var questionDataJson = question.QuestionDataJson as string;

        // 情况1: QuestionDataJson 为空，但有旧字段数据
        if (string.IsNullOrWhiteSpace(questionDataJson))
        {
            var hasLegacyData = !string.IsNullOrWhiteSpace(question.Options)
                || !string.IsNullOrWhiteSpace(question.CorrectAnswer);

            if (hasLegacyData)
            {
                return new DataConsistencyIssue
                {
                    QuestionId = question.Id,
                    IssueType = "MissingQuestionDataJson",
                    Description = "QuestionDataJson 为空，但存在旧字段数据",
                    Severity = "Warning",
                    Recommendation = "运行数据迁移脚本将旧字段迁移到 QuestionDataJson"
                };
            }

            return new DataConsistencyIssue
            {
                QuestionId = question.Id,
                IssueType = "NoDataAtAll",
                Description = "题目没有任何数据（新字段和旧字段都为空）",
                Severity = "Error",
                Recommendation = "删除或补充此题目的数据"
            };
        }

        // 情况2: QuestionDataJson 不为空，检查是否能正确解析
        try
        {
            using var jsonDoc = JsonDocument.Parse(questionDataJson);
            var root = jsonDoc.RootElement;

            // 检查是否有 type 字段
            if (!root.TryGetProperty("type", out var typeProperty))
            {
                return new DataConsistencyIssue
                {
                    QuestionId = question.Id,
                    IssueType = "MissingTypeInData",
                    Description = "QuestionDataJson 缺少 type 字段",
                    Severity = "Error",
                    Recommendation = "补充 QuestionDataJson 的 type 字段"
                };
            }

            var dataType = typeProperty.GetString();

            // 检查 type 字段与 QuestionType 是否一致
            var expectedType = dataType == "choice"
                ? GetExpectedChoiceQuestionType(root)
                : GetExpectedQuestionType(dataType);
            if (!string.Equals(expectedType, questionType, StringComparison.OrdinalIgnoreCase))
            {
                return new DataConsistencyIssue
                {
                    QuestionId = question.Id,
                    IssueType = "TypeMismatch",
                    Description = $"QuestionType ({questionType}) 与 QuestionDataJson.type ({dataType}) 不匹配",
                    Severity = "Error",
                    Recommendation = $"统一 QuestionType 与 QuestionDataJson.type"
                };
            }
        }
        catch (JsonException ex)
        {
            return new DataConsistencyIssue
            {
                QuestionId = question.Id,
                IssueType = "InvalidJson",
                Description = $"QuestionDataJson 不是有效的 JSON: {ex.Message}",
                Severity = "Error",
                Recommendation = "修复 QuestionDataJson 的 JSON 格式"
            };
        }

        return null;
    }

    /// <summary>
    /// 根据 QuestionDataJson 的 type 字段获取期望的 QuestionType
    /// </summary>
    private static string GetExpectedQuestionType(string? dataType)
    {
        return dataType switch
        {
            // 新格式（小写 discriminator，与 JsonDerivedType 一致）
            "choice" => "SingleChoice",
            "boolean" => "TrueFalse",
            "fillBlank" => "FillBlank",
            "shortAnswer" => "ShortAnswer",
            // 兼容旧格式（已迁移的历史数据）
            "ChoiceQuestionData" => "MultipleChoice",
            "BooleanQuestionData" => "TrueFalse",
            "FillBlankQuestionData" => "FillBlank",
            "ShortAnswerQuestionData" => "ShortAnswer",
            _ => string.Empty
        };
    }

    private static string GetExpectedChoiceQuestionType(JsonElement root)
    {
        if (root.TryGetProperty("correctAnswers", out var correctAnswers) &&
            correctAnswers.ValueKind == JsonValueKind.Array)
        {
            var count = 0;
            foreach (var _ in correctAnswers.EnumerateArray())
            {
                count++;
                if (count > 1)
                {
                    return "MultipleChoice";
                }
            }
            return "SingleChoice";
        }

        return "SingleChoice";
    }
}

/// <summary>
/// 数据一致性报告
/// </summary>
public class DataConsistencyReport
{
    public int TotalQuestions { get; set; }
    public int InconsistentQuestions { get; set; }
    public double ConsistencyRate { get; set; }
    public List<DataConsistencyIssue> Issues { get; set; } = new();

    public bool IsHealthy => InconsistentQuestions == 0;
}

/// <summary>
/// 数据一致性问题
/// </summary>
public class DataConsistencyIssue
{
    public int QuestionId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Error, Warning
    public string Recommendation { get; set; } = string.Empty;
}
