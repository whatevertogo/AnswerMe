using AnswerMe.Application.DTOs;
using AnswerMe.Application.AI;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// AI生成题目服务接口
/// </summary>
public interface IAIGenerationService
{
    /// <summary>
    /// 生成题目（同步，≤20题）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="dto">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的题目</returns>
    Task<AIGenerateResponseDto> GenerateQuestionsAsync(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步生成题目（>20题）
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="dto">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务ID，用于查询进度</returns>
    Task<string> StartAsyncGeneration(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询生成进度
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成进度</returns>
    Task<AIGenerateProgressDto?> GetProgressAsync(
        int userId,
        string taskId,
        CancellationToken cancellationToken = default);
}
