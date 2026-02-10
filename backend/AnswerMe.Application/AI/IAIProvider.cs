namespace AnswerMe.Application.AI;

/// <summary>
/// AI Provider接口
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Provider名称
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 生成题目
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="request">生成请求</param>
    /// <param name="model">模型名称（可选，如果为空则使用 Provider 默认模型）</param>
    /// <param name="endpoint">自定义端点（可选，仅用于 custom_api 类型）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的题目列表</returns>
    Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        string apiKey,
        AIQuestionGenerateRequest request,
        string? model = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证API密钥是否有效
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="endpoint">自定义端点（可选，部分Provider需要）</param>
    /// <param name="model">模型名称（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有效</returns>
    Task<bool> ValidateApiKeyAsync(
        string apiKey,
        string? endpoint = null,
        string? model = null,
        CancellationToken cancellationToken = default);
}
