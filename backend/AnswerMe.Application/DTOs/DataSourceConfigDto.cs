namespace AnswerMe.Application.DTOs;

/// <summary>
/// 数据源配置DTO（包含解密后的敏感信息）
/// </summary>
public class DataSourceConfigDto
{
    /// <summary>
    /// API密钥（解密后）
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 自定义端点（可选）
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 模型名称（可选，如果为空则使用 Provider 默认模型）
    /// </summary>
    public string? Model { get; set; }
}
