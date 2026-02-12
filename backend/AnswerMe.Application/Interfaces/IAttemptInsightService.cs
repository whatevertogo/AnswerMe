using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 答题复盘与 AI 建议服务接口
/// </summary>
public interface IAttemptInsightService
{
    /// <summary>
    /// 生成答题 AI 学习建议
    /// </summary>
    Task<AttemptAiSuggestionDto> GenerateAiSuggestionAsync(
        int userId,
        int attemptId,
        CancellationToken cancellationToken = default);
}
