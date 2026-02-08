namespace AnswerMe.Application.DTOs;

/// <summary>
/// 速率限制配置选项
/// </summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>
    /// 是否启用端点级别的速率限制
    /// </summary>
    public bool EnableEndpointRateLimiting { get; set; } = true;

    /// <summary>
    /// 是否在响应头中添加速率限制信息
    /// </summary>
    public bool AddRateLimitHeaders { get; set; } = true;

    /// <summary>
    /// 是否堆叠被阻塞的请求
    /// </summary>
    public bool StackBlockedRequests { get; set; } = false;

    /// <summary>
    /// IP头名称(用于代理场景)
    /// </summary>
    public string? IpHeaderName { get; set; }

    /// <summary>
    /// Token桶配置
    /// </summary>
    public TokenBucketOptions? TokenBucket { get; set; }
}

/// <summary>
/// Token桶速率限制选项
/// </summary>
public class TokenBucketOptions
{
    /// <summary>
    /// 补充周期(秒)
    /// </summary>
    public int ReplenishPeriod { get; set; } = 1;

    /// <summary>
    /// Token限制
    /// </summary>
    public int TokenLimit { get; set; } = 10;

    /// <summary>
    /// 桶容量
    /// </summary>
    public int BucketCapacity { get; set; } = 10;
}
