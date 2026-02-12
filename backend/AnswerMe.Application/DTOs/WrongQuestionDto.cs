namespace AnswerMe.Application.DTOs;

/// <summary>
/// 错题查询DTO
/// </summary>
public class WrongQuestionQueryDto
{
    public int? QuestionBankId { get; set; }
    public string? QuestionType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 错题DTO
/// </summary>
public class WrongQuestionDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public int AttemptId { get; set; }
    public DateTime AnsweredAt { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public bool IsMastered { get; set; }
}

/// <summary>
/// 错题列表响应DTO
/// </summary>
public class WrongQuestionListDto
{
    public List<WrongQuestionDto> Questions { get; set; } = new();
    public int TotalCount { get; set; }
    public int BankGroupCount { get; set; }
}

/// <summary>
/// 学习统计DTO
/// </summary>
public class LearningStatsDto
{
    public int TotalAttempts { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public decimal AccuracyRate { get; set; }
    public decimal AverageTimePerQuestion { get; set; }
    public int TotalTimeSpent { get; set; }
    public List<WeeklyStatDto> WeeklyTrend { get; set; } = new();
    public List<BankStatDto> BankStats { get; set; } = new();
}

/// <summary>
/// 每周统计DTO
/// </summary>
public class WeeklyStatDto
{
    public DateTime WeekStart { get; set; }
    public int AttemptCount { get; set; }
    public int QuestionCount { get; set; }
    public int CorrectCount { get; set; }
    public decimal AccuracyRate { get; set; }
}

/// <summary>
/// 题库统计DTO
/// </summary>
public class BankStatDto
{
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int AccuracyRate { get; set; }
}
