using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Services;

/// <summary>
/// 统计服务实现
/// </summary>
public class StatsService : IStatsService
{
    private readonly IUserRepository _userRepository;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IAttemptRepository _attemptRepository;
    private readonly IDataSourceRepository _dataSourceRepository;

    public StatsService(
        IUserRepository userRepository,
        IQuestionBankRepository questionBankRepository,
        IQuestionRepository questionRepository,
        IAttemptRepository attemptRepository,
        IDataSourceRepository dataSourceRepository)
    {
        _userRepository = userRepository;
        _questionBankRepository = questionBankRepository;
        _questionRepository = questionRepository;
        _attemptRepository = attemptRepository;
        _dataSourceRepository = dataSourceRepository;
    }

    public async Task<HomeStatsDto> GetHomeStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        // 获取题库总数
        var questionBanks = await _questionBankRepository.GetByUserIdAsync(userId, cancellationToken);
        int questionBanksCount = questionBanks.Count;

        // 获取题目总数
        var questionBankIds = questionBanks.Select(qb => qb.Id).ToList();
        var questionCountMap = await _questionRepository.CountByQuestionBankIdsAsync(questionBankIds, cancellationToken);
        var questionsCount = questionCountMap.Values.Sum();

        // 获取本月练习次数
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthlyAttempts = await _attemptRepository.CountByUserIdAndDateRangeAsync(
            userId, monthStart, now, cancellationToken);

        // 获取AI数据源数量
        var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);
        int dataSourcesCount = dataSources.Count;

        return new HomeStatsDto
        {
            QuestionBanksCount = questionBanksCount,
            QuestionsCount = questionsCount,
            MonthlyAttempts = monthlyAttempts,
            DataSourcesCount = dataSourcesCount
        };
    }

    public async Task<List<HomeRecentActivityDto>> GetHomeRecentActivitiesAsync(
        int userId,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return new List<HomeRecentActivityDto>();
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var actorName = !string.IsNullOrWhiteSpace(user?.Username)
            ? user!.Username
            : user?.Email ?? $"用户#{userId}";

        var questionBanks = await _questionBankRepository.GetByUserIdAsync(userId, cancellationToken);
        var questionBankMap = questionBanks.ToDictionary(qb => qb.Id, qb => qb);
        var bankIds = questionBanks.Select(qb => qb.Id).ToList();

        var activities = new List<HomeRecentActivityDto>();

        activities.AddRange(questionBanks
            .OrderByDescending(qb => qb.CreatedAt)
            .Take(limit)
            .Select(qb => new HomeRecentActivityDto
            {
                Type = "create_bank",
                Title = $"{actorName} 创建了「{qb.Name}」题库",
                OccurredAt = qb.CreatedAt
            }));

        if (bankIds.Count > 0)
        {
            var recentQuestions = await _questionRepository.GetRecentByQuestionBankIdsAsync(
                bankIds,
                take: Math.Max(limit * 10, 30),
                cancellationToken: cancellationToken);

            var generationActivities = recentQuestions
                .GroupBy(q => new
                {
                    q.QuestionBankId,
                    Bucket = q.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .OrderByDescending(g => g.Max(x => x.CreatedAt))
                .Take(limit)
                .Select(g =>
                {
                    var questionBankName = questionBankMap.TryGetValue(g.Key.QuestionBankId, out var questionBank)
                        ? questionBank.Name
                        : $"题库#{g.Key.QuestionBankId}";

                    return new HomeRecentActivityDto
                    {
                        Type = "generate_questions",
                        Title = $"{actorName} 在「{questionBankName}」生成了 {g.Count()} 道题目",
                        OccurredAt = g.Max(x => x.CreatedAt)
                    };
                });

            activities.AddRange(generationActivities);
        }

        var completedAttempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);
        activities.AddRange(completedAttempts
            .Where(a => a.CompletedAt.HasValue)
            .OrderByDescending(a => a.CompletedAt)
            .Take(limit)
            .Select(a => new HomeRecentActivityDto
            {
                Type = "complete_practice",
                Title = $"{actorName} 完成了「{a.QuestionBank?.Name ?? $"题库#{a.QuestionBankId}"}」练习",
                OccurredAt = a.CompletedAt!.Value
            }));

        return activities
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .ToList();
    }
}
