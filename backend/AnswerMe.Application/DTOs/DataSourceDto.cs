namespace AnswerMe.Application.DTOs;

/// <summary>
/// 创建数据源DTO
/// </summary>
public class CreateDataSourceDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // openai, qwen, custom_api
    public string ApiKey { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public bool IsDefault { get; set; } = false;
}

/// <summary>
/// 更新数据源DTO
/// </summary>
public class UpdateDataSourceDto
{
    public string? Name { get; set; }
    public string? ApiKey { get; set; }
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public bool? IsDefault { get; set; }
}

/// <summary>
/// 数据源响应DTO
/// </summary>
public class DataSourceDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public string MaskedApiKey { get; set; } = string.Empty; // 脱敏的API密钥
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// API使用统计DTO
/// </summary>
public class ApiUsageStatsDto
{
    public int DataSourceId { get; set; }
    public string DataSourceName { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public decimal TotalCost { get; set; }
    public DateTime LastUsed { get; set; }
}
