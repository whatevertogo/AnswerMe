using System.Text;
using AnswerMe.Application.AI;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Models;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Application.Services;

/// <summary>
/// 答题复盘与 AI 建议服务
/// </summary>
public class AttemptInsightService : IAttemptInsightService
{
    private readonly IAttemptRepository _attemptRepository;
    private readonly IAttemptDetailRepository _attemptDetailRepository;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataSourceService _dataSourceService;
    private readonly AIProviderFactory _aiProviderFactory;
    private readonly ILogger<AttemptInsightService> _logger;

    public AttemptInsightService(
        IAttemptRepository attemptRepository,
        IAttemptDetailRepository attemptDetailRepository,
        IQuestionBankRepository questionBankRepository,
        IDataSourceRepository dataSourceRepository,
        IDataSourceService dataSourceService,
        AIProviderFactory aiProviderFactory,
        ILogger<AttemptInsightService> logger)
    {
        _attemptRepository = attemptRepository;
        _attemptDetailRepository = attemptDetailRepository;
        _questionBankRepository = questionBankRepository;
        _dataSourceRepository = dataSourceRepository;
        _dataSourceService = dataSourceService;
        _aiProviderFactory = aiProviderFactory;
        _logger = logger;
    }

    public async Task<AttemptAiSuggestionDto> GenerateAiSuggestionAsync(
        int userId,
        int attemptId,
        CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            throw new InvalidOperationException("答题记录不存在或无权访问");
        }

        if (!attempt.CompletedAt.HasValue)
        {
            throw new InvalidOperationException("答题尚未完成，暂时无法生成复盘建议");
        }

        var questionBank = await _questionBankRepository.GetByIdAsync(attempt.QuestionBankId, cancellationToken);
        var details = await _attemptDetailRepository.GetByAttemptIdWithQuestionsAsync(attemptId, cancellationToken);
        var overview = BuildOverview(attempt, details);
        var weakPoints = BuildWeakPoints(details);

        var (providerName, dataSourceName, aiQuestion) =
            await GenerateSuggestionQuestionAsync(
                userId,
                questionBank?.Name ?? $"题库 #{attempt.QuestionBankId}",
                overview,
                weakPoints,
                cancellationToken);

        var summary = BuildSummary(aiQuestion, overview, weakPoints);
        var suggestions = BuildSuggestions(aiQuestion, overview, weakPoints);
        var studyPlan = BuildStudyPlan(aiQuestion, overview, weakPoints);

        return new AttemptAiSuggestionDto
        {
            AttemptId = attempt.Id,
            QuestionBankId = attempt.QuestionBankId,
            QuestionBankName = questionBank?.Name ?? string.Empty,
            Overview = overview,
            WeakPoints = weakPoints,
            Summary = summary,
            Suggestions = suggestions,
            StudyPlan = studyPlan,
            ProviderName = providerName,
            DataSourceName = dataSourceName,
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static AttemptOverviewDto BuildOverview(Attempt attempt, List<AttemptDetail> details)
    {
        var answered = details.Count(detail => !string.IsNullOrWhiteSpace(detail.UserAnswer));
        var correct = details.Count(detail => detail.IsCorrect == true);
        var incorrect = Math.Max(answered - correct, 0);
        var unanswered = Math.Max(attempt.TotalQuestions - answered, 0);
        var accuracyRate = attempt.TotalQuestions > 0
            ? Math.Round((decimal)correct / attempt.TotalQuestions * 100, 2)
            : 0;

        var answeredWithTime = details
            .Where(detail => !string.IsNullOrWhiteSpace(detail.UserAnswer) && detail.TimeSpent.HasValue && detail.TimeSpent.Value > 0)
            .Select(detail => detail.TimeSpent!.Value)
            .ToList();

        var avgTime = answeredWithTime.Count > 0
            ? (int)Math.Round(answeredWithTime.Average())
            : 0;

        return new AttemptOverviewDto
        {
            TotalQuestions = attempt.TotalQuestions,
            AnsweredQuestions = answered,
            CorrectQuestions = correct,
            IncorrectQuestions = incorrect,
            UnansweredQuestions = unanswered,
            AccuracyRate = accuracyRate,
            DurationSeconds = (int)(attempt.CompletedAt!.Value - attempt.StartedAt).TotalSeconds,
            AverageTimePerAnswered = avgTime
        };
    }

    private static List<AttemptWeakPointDto> BuildWeakPoints(List<AttemptDetail> details)
    {
        var weakPoints = new List<AttemptWeakPointDto>();

        var typeWeakPoints = details
            .Where(detail => detail.Question != null)
            .GroupBy(detail => GetTypeDisplayName(detail.Question.QuestionTypeEnum, detail.Question.QuestionType))
            .Select(group =>
            {
                var total = group.Count();
                var incorrect = group.Count(item => item.IsCorrect == false);
                var accuracy = total > 0
                    ? Math.Round((decimal)(total - incorrect) / total * 100, 2)
                    : 0;
                return new AttemptWeakPointDto
                {
                    Category = "题型",
                    Name = group.Key,
                    Total = total,
                    Incorrect = incorrect,
                    AccuracyRate = accuracy
                };
            })
            .Where(item => item.Total >= 2 || item.Incorrect > 0)
            .OrderBy(item => item.AccuracyRate)
            .ThenByDescending(item => item.Incorrect)
            .Take(3);

        weakPoints.AddRange(typeWeakPoints);

        var difficultyWeakPoints = details
            .Where(detail => detail.Question != null && !string.IsNullOrWhiteSpace(detail.Question.Difficulty))
            .GroupBy(detail => NormalizeDifficulty(detail.Question.Difficulty))
            .Select(group =>
            {
                var total = group.Count();
                var incorrect = group.Count(item => item.IsCorrect == false);
                var accuracy = total > 0
                    ? Math.Round((decimal)(total - incorrect) / total * 100, 2)
                    : 0;
                return new AttemptWeakPointDto
                {
                    Category = "难度",
                    Name = group.Key,
                    Total = total,
                    Incorrect = incorrect,
                    AccuracyRate = accuracy
                };
            })
            .Where(item => item.Total >= 2 || item.Incorrect > 0)
            .OrderBy(item => item.AccuracyRate)
            .ThenByDescending(item => item.Incorrect)
            .Take(2);

        weakPoints.AddRange(difficultyWeakPoints);

        return weakPoints
            .OrderBy(item => item.AccuracyRate)
            .ThenByDescending(item => item.Incorrect)
            .Take(5)
            .ToList();
    }

    private async Task<(string providerName, string dataSourceName, GeneratedQuestion aiQuestion)> GenerateSuggestionQuestionAsync(
        int userId,
        string questionBankName,
        AttemptOverviewDto overview,
        List<AttemptWeakPointDto> weakPoints,
        CancellationToken cancellationToken)
    {
        var dataSource = await _dataSourceRepository.GetDefaultByUserIdAsync(userId, cancellationToken);
        if (dataSource == null)
        {
            var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);
            dataSource = dataSources.FirstOrDefault();
        }

        if (dataSource == null)
        {
            throw new InvalidOperationException("未找到可用 AI 配置，请先到「AI配置」页面添加并验证数据源");
        }

        var config = await _dataSourceService.GetDecryptedConfigAsync(dataSource.Id, userId, cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.ApiKey))
        {
            throw new InvalidOperationException("AI 配置无效，请重新填写 API Key 后重试");
        }

        var provider = _aiProviderFactory.GetProvider(dataSource.Type);
        if (provider == null)
        {
            throw new InvalidOperationException($"不支持的 AI Provider: {dataSource.Type}");
        }

        var request = new AIQuestionGenerateRequest
        {
            Subject = $"{questionBankName} 答题复盘",
            Count = 1,
            Difficulty = "medium",
            QuestionTypes = new List<QuestionType> { QuestionType.ShortAnswer },
            Language = "zh-CN",
            CustomPrompt = BuildInsightPrompt(overview, weakPoints)
        };

        var response = await provider.GenerateQuestionsAsync(
            config.ApiKey,
            request,
            config.Model,
            config.Endpoint,
            cancellationToken);

        if (!response.Success || response.Questions.Count == 0)
        {
            _logger.LogWarning(
                "生成答题 AI 建议失败: Provider={Provider}, ErrorCode={ErrorCode}, Error={Error}",
                provider.ProviderName,
                response.ErrorCode,
                response.ErrorMessage);
            throw new InvalidOperationException($"AI 建议生成失败：{response.ErrorMessage ?? "请稍后重试"}");
        }

        return (provider.ProviderName, dataSource.Name, response.Questions[0]);
    }

    private static string BuildInsightPrompt(AttemptOverviewDto overview, List<AttemptWeakPointDto> weakPoints)
    {
        var weakPointText = weakPoints.Count > 0
            ? string.Join("；", weakPoints.Select(point =>
                $"{point.Category}-{point.Name}(错误{point.Incorrect}/{point.Total}, 准确率{point.AccuracyRate:F2}%)"))
            : "暂无明显薄弱点";

        return $"""
你不是出题，而是做学习复盘。请基于以下答题数据给出学习建议，并严格按“1道简答题”格式返回：
- questionText: 40~120字，输出总体诊断总结（不要提“题目/答案”字样）
- correctAnswer: 输出3~5条可执行建议，每条单独一行，以“- ”开头
- explanation: 输出一个7天复习计划（分天简洁描述）
- options: 返回空数组 []
- difficulty: 固定为 "medium"

答题数据：
- 总题数: {overview.TotalQuestions}
- 已作答: {overview.AnsweredQuestions}
- 答对: {overview.CorrectQuestions}
- 答错: {overview.IncorrectQuestions}
- 未作答: {overview.UnansweredQuestions}
- 总体准确率: {overview.AccuracyRate:F2}%
- 平均每题用时: {overview.AverageTimePerAnswered} 秒
- 薄弱点: {weakPointText}

要求：
1. 必须可执行、可量化，避免空泛建议。
2. 优先针对薄弱点给出训练动作。
3. 全部输出使用中文。
""";
    }

    private static string BuildSummary(
        GeneratedQuestion aiQuestion,
        AttemptOverviewDto overview,
        List<AttemptWeakPointDto> weakPoints)
    {
        if (!string.IsNullOrWhiteSpace(aiQuestion.QuestionText))
        {
            return aiQuestion.QuestionText.Trim();
        }

        var weakPointPart = weakPoints.Count > 0
            ? $"当前薄弱点集中在：{string.Join("、", weakPoints.Select(point => $"{point.Category}{point.Name}"))}。"
            : "当前答题结构较均衡。";

        return $"本次共完成 {overview.AnsweredQuestions}/{overview.TotalQuestions} 题，准确率 {overview.AccuracyRate:F2}%。{weakPointPart}";
    }

    private static List<string> BuildSuggestions(
        GeneratedQuestion aiQuestion,
        AttemptOverviewDto overview,
        List<AttemptWeakPointDto> weakPoints)
    {
        var source = ExtractSuggestionSource(aiQuestion);
        var parsed = SplitToBulletList(source);
        if (parsed.Count > 0)
        {
            return parsed.Take(5).ToList();
        }

        var fallback = new List<string>();
        foreach (var point in weakPoints.Take(3))
        {
            fallback.Add($"围绕{point.Category}「{point.Name}」做10~15题专项训练，完成后复盘错因并记录一条改进规则。");
        }

        if (overview.UnansweredQuestions > 0)
        {
            fallback.Add($"优先提升答题完成度，下一次练习确保未作答题数从 {overview.UnansweredQuestions} 降到 0。");
        }

        if (overview.AverageTimePerAnswered > 90)
        {
            fallback.Add("单题思考超过90秒时先做标记并跳过，二轮回看，提升整体节奏。");
        }

        if (fallback.Count == 0)
        {
            fallback.Add("保持每日20分钟复盘，聚焦错题原因和对应知识点，持续巩固正确率。");
        }

        return fallback.Take(5).ToList();
    }

    private static string BuildStudyPlan(
        GeneratedQuestion aiQuestion,
        AttemptOverviewDto overview,
        List<AttemptWeakPointDto> weakPoints)
    {
        var explanation = aiQuestion.Explanation?.Trim();
        if (!string.IsNullOrWhiteSpace(explanation))
        {
            return explanation;
        }

        var focus = weakPoints.Count > 0
            ? string.Join("、", weakPoints.Select(point => $"{point.Category}{point.Name}"))
            : "错题复盘";

        return $"第1-2天：回顾本次错题并整理知识点；第3-5天：围绕{focus}做专项练习；第6-7天：完成一次整套模拟并复盘用时与错误类型。";
    }

    private static string ExtractSuggestionSource(GeneratedQuestion aiQuestion)
    {
        if (aiQuestion.Data is ShortAnswerQuestionData shortAnswer &&
            !string.IsNullOrWhiteSpace(shortAnswer.ReferenceAnswer))
        {
            return shortAnswer.ReferenceAnswer;
        }

#pragma warning disable CS0618 // 旧字段兼容性代码
        if (!string.IsNullOrWhiteSpace(aiQuestion.CorrectAnswer))
        {
            return aiQuestion.CorrectAnswer;
        }
#pragma warning restore CS0618

        return aiQuestion.Explanation ?? string.Empty;
    }

    private static List<string> SplitToBulletList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<string>();
        }

        var builder = new StringBuilder(raw);
        builder.Replace("；", "\n");
        builder.Replace("。", "\n");

        return builder
            .ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Trim())
            .Select(line => line.TrimStart('-', '•', '·', '*', ' '))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct()
            .ToList();
    }

    private static string NormalizeDifficulty(string difficulty)
    {
        return difficulty.Trim().ToLowerInvariant() switch
        {
            "easy" => "简单",
            "medium" => "中等",
            "hard" => "困难",
            _ => difficulty
        };
    }

    private static string GetTypeDisplayName(QuestionType? type, string legacyType)
    {
        if (type.HasValue && Enum.IsDefined(typeof(QuestionType), type.Value))
        {
            return type.Value.DisplayName();
        }

        var parsed = QuestionTypeExtensions.ParseFromString(legacyType);
        return parsed?.DisplayName() ?? "未知题型";
    }
}
