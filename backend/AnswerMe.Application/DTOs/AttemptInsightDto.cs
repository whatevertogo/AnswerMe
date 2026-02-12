namespace AnswerMe.Application.DTOs;

/// <summary>
/// 答题概览统计
/// </summary>
public class AttemptOverviewDto
{
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int CorrectQuestions { get; set; }
    public int IncorrectQuestions { get; set; }
    public int UnansweredQuestions { get; set; }
    public decimal AccuracyRate { get; set; }
    public int DurationSeconds { get; set; }
    public int AverageTimePerAnswered { get; set; }
}

/// <summary>
/// 薄弱点统计
/// </summary>
public class AttemptWeakPointDto
{
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Incorrect { get; set; }
    public decimal AccuracyRate { get; set; }
}

/// <summary>
/// AI 学习建议结果
/// </summary>
public class AttemptAiSuggestionDto
{
    public int AttemptId { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public AttemptOverviewDto Overview { get; set; } = new();
    public List<AttemptWeakPointDto> WeakPoints { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public List<string> Suggestions { get; set; } = new();
    public string StudyPlan { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string DataSourceName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
}
