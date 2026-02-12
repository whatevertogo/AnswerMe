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

/// <summary>
/// 首页最近活动DTO
/// </summary>
public class HomeRecentActivityDto
{
    /// <summary>
    /// 活动类型：create_bank / generate_questions / complete_practice
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 活动文案
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 活动发生时间（UTC）
    /// </summary>
    public DateTime OccurredAt { get; set; }
}
