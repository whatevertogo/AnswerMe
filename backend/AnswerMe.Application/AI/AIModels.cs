namespace AnswerMe.Application.AI;

/// <summary>
/// AI题目生成请求
/// </summary>
public class AIQuestionGenerateRequest
{
    public string Subject { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Difficulty { get; set; } = "medium";
    public List<string> QuestionTypes { get; set; } = new();
    public string? CustomPrompt { get; set; }
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// AI题目生成响应
/// </summary>
public class AIQuestionGenerateResponse
{
    public bool Success { get; set; }
    public List<GeneratedQuestion> Questions { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public int? TokensUsed { get; set; }
}

/// <summary>
/// 生成的题目
/// </summary>
public class GeneratedQuestion
{
    public string QuestionType { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
}
