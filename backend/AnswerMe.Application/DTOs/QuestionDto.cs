namespace AnswerMe.Application.DTOs;

/// <summary>
/// 创建题目DTO
/// </summary>
public class CreateQuestionDto
{
    public int QuestionBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty; // choice, fill_blank, essay
    public string? Options { get; set; } // JSON数组
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium"; // easy, medium, hard
    public int OrderIndex { get; set; }
}

/// <summary>
/// 更新题目DTO
/// </summary>
public class UpdateQuestionDto
{
    public string? QuestionText { get; set; }
    public string? QuestionType { get; set; }
    public string? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? Explanation { get; set; }
    public string? Difficulty { get; set; }
    public int? OrderIndex { get; set; }
}

/// <summary>
/// 题目响应DTO
/// </summary>
public class QuestionDto
{
    public int Id { get; set; }
    public int QuestionBankId { get; set; }
    public string QuestionBankName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string? Options { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium";
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 题目列表查询参数
/// </summary>
public class QuestionListQueryDto
{
    public int QuestionBankId { get; set; }
    public string? Search { get; set; }
    public string? Difficulty { get; set; }
    public string? QuestionType { get; set; }
    public int PageSize { get; set; } = 20;
    public int? LastId { get; set; } // 游标分页
}

/// <summary>
/// 题目列表响应DTO
/// </summary>
public class QuestionListDto
{
    public List<QuestionDto> Data { get; set; } = new();
    public bool HasMore { get; set; }
    public int? NextCursor { get; set; }
    public int TotalCount { get; set; }
}
