namespace AnswerMe.Application.DTOs;

/// <summary>
/// 首页统计数据DTO
/// </summary>
public class HomeStatsDto
{
    /// <summary>
    /// 题库总数
    /// </summary>
    public int QuestionBanksCount { get; set; }

    /// <summary>
    /// 题目总数
    /// </summary>
    public int QuestionsCount { get; set; }

    /// <summary>
    /// 本月练习次数
    /// </summary>
    public int MonthlyAttempts { get; set; }

    /// <summary>
    /// AI数据源数量
    /// </summary>
    public int DataSourcesCount { get; set; }
}
