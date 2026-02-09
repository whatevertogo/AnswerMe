namespace AnswerMe.Application.DTOs;

/// <summary>
/// 开始答题DTO
/// </summary>
public class StartAttemptDto
{
    public int QuestionBankId { get; set; }
    public string? Mode { get; set; } // "sequential" 或 "random"
}

/// <summary>
/// 提交答案DTO
/// </summary>
public class SubmitAnswerDto
{
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string UserAnswer { get; set; } = string.Empty;
    public int? TimeSpent { get; set; } // 可选,记录答题用时
}

/// <summary>
/// 完成答题DTO
/// </summary>
public class CompleteAttemptDto
{
    public int AttemptId { get; set; }
}

/// <summary>
/// 答题记录响应DTO
/// </summary>
public class AttemptDto
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal? Score { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int? DurationSeconds { get; set; }
}

/// <summary>
/// 答题详情响应DTO
/// </summary>
public class AttemptDetailDto
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public string? UserAnswer { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public int? TimeSpent { get; set; }
    public string? Explanation { get; set; }
}

/// <summary>
/// 开始答题响应DTO
/// </summary>
public class StartAttemptResponseDto
{
    public int AttemptId { get; set; }
    public List<int> QuestionIds { get; set; } = new();
}

/// <summary>
/// 答题统计DTO
/// </summary>
public class AttemptStatisticsDto
{
    public int TotalAttempts { get; set; }
    public int CompletedAttempts { get; set; }
    public decimal? AverageScore { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public decimal? OverallAccuracy { get; set; }
}
