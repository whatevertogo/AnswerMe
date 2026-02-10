using Microsoft.Extensions.Logging;
using System.Net;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// HTTP 请求重试辅助类
/// 实现指数退避策略，用于处理 API 限流和临时错误
/// </summary>
public static class HttpRetryHelper
{
    /// <summary>
    /// 最大重试次数
    /// </summary>
    private const int MaxRetries = 3;

    /// <summary>
    /// 基础延迟时间（秒）
    /// </summary>
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// 可重试的 HTTP 状态码
    /// </summary>
    private static readonly HashSet<HttpStatusCode> RetryableStatusCodes = new()
    {
        HttpStatusCode.TooManyRequests,      // 429 - 限流
        HttpStatusCode.ServiceUnavailable,    // 503 - 服务不可用
        HttpStatusCode.GatewayTimeout         // 504 - 网关超时
    };

    /// <summary>
    /// 使用指数退避策略发送 HTTP 请求
    /// </summary>
    /// <param name="httpClient">HTTP 客户端</param>
    /// <param name="request">HTTP 请求消息</param>
    /// <param name="logger">日志记录器</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP 响应消息</returns>
    /// <exception cref="HttpRequestException">重试次数用尽后抛出</exception>
    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient httpClient,
        HttpRequestMessage request,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                // 对于重试，需要重新创建请求（因为 HttpRequestMessage 不能重复使用）
                var currentRequest = attempt == 0
                    ? request
                    : CloneHttpRequestMessage(request);

                var response = await httpClient.SendAsync(currentRequest, cancellationToken);

                // 检查是否需要重试
                if (RetryableStatusCodes.Contains(response.StatusCode))
                {
                    if (attempt < MaxRetries)
                    {
                        // 计算指数退避延迟
                        var delay = BaseDelay * Math.Pow(2, attempt);
                        var delaySeconds = ((TimeSpan)delay).TotalSeconds;

                        logger.LogWarning(
                            "API 请求失败 {StatusCode}，{Delay}秒后重试（第 {Attempt}/{MaxRetries} 次）...",
                            response.StatusCode,
                            delaySeconds,
                            attempt + 1,
                            MaxRetries);

                        // 读取响应内容并记录（帮助调试）
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        logger.LogDebug("错误响应内容: {Body}", responseBody);

                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }
                    else
                    {
                        logger.LogError(
                            "API 请求失败 {StatusCode}，已达到最大重试次数 {MaxRetries}",
                            response.StatusCode,
                            MaxRetries);
                    }
                }

                // 成功或非可重试状态码，直接返回
                return response;
            }
            catch (HttpRequestException ex)
            {
                if (attempt == MaxRetries)
                {
                    logger.LogError(ex, "HTTP 请求失败，已达到最大重试次数");
                    throw;
                }

                logger.LogWarning(ex, "HTTP 请求失败，{Delay}秒后重试...", BaseDelay.TotalSeconds);
                await Task.Delay(BaseDelay, cancellationToken);
            }
        }

        // 理论上不应该到达这里
        throw new HttpRequestException("重试次数已用尽");
    }

    /// <summary>
    /// 克隆 HTTP 请求消息（用于重试）
    /// </summary>
    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage original)
    {
        var clone = new HttpRequestMessage(original.Method, original.RequestUri);

        // 复制 Headers
        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // 复制 Content
        if (original.Content != null)
        {
            clone.Content = new StreamContent(original.Content.ReadAsStream());
            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return clone;
    }
}
