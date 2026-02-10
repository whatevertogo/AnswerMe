using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Services;

/// <summary>
/// 统计服务实现
/// </summary>
public class StatsService : IStatsService
{
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IAttemptRepository _attemptRepository;
    private readonly IDataSourceRepository _dataSourceRepository;

    public StatsService(
        IQuestionBankRepository questionBankRepository,
        IQuestionRepository questionRepository,
        IAttemptRepository attemptRepository,
        IDataSourceRepository dataSourceRepository)
    {
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
        int questionsCount = 0;
        foreach (var bankId in questionBankIds)
        {
            var count = await _questionRepository.CountByQuestionBankIdAsync(bankId, cancellationToken);
            questionsCount += count;
        }

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
}
