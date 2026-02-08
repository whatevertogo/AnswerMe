namespace AnswerMe.Application.DTOs;

/// <summary>
/// 创建题库DTO
/// </summary>
public class CreateQuestionBankDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public int? DataSourceId { get; set; }
}

/// <summary>
/// 更新题库DTO
/// </summary>
public class UpdateQuestionBankDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? Tags { get; set; }
    public int? DataSourceId { get; set; }
    public byte[]? Version { get; set; } // 乐观锁版本号
}

/// <summary>
/// 题库响应DTO
/// </summary>
public class QuestionBankDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public int? DataSourceId { get; set; }
    public string? DataSourceName { get; set; }
    public int QuestionCount { get; set; }
    public byte[] Version { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 题库列表查询参数
/// </summary>
public class QuestionBankListQueryDto
{
    public string? Search { get; set; }
    public string? Tag { get; set; }
    public int PageSize { get; set; } = 20;
    public int? LastId { get; set; } // 游标分页
}

/// <summary>
/// 题库列表响应DTO
/// </summary>
public class QuestionBankListDto
{
    public List<QuestionBankDto> Data { get; set; } = new();
    public bool HasMore { get; set; }
    public int? NextCursor { get; set; }
}
