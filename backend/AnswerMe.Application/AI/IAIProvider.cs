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
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的题目列表</returns>
    Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        AIQuestionGenerateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证API密钥是否有效
    /// </summary>
    /// <param name="apiKey">API密钥</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否有效</returns>
    Task<bool> ValidateApiKeyAsync(
        string apiKey,
        CancellationToken cancellationToken = default);
}
