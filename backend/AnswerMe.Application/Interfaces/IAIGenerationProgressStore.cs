using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// AI 生成任务进度存储接口
/// </summary>
public interface IAIGenerationProgressStore
{
    /// <summary>
    /// 获取任务进度
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<AIGenerateProgressDto?> GetAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置任务进度
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="progress">进度信息</param>
    /// <param name="ttl">过期时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetAsync(string taskId, AIGenerateProgressDto progress, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新任务进度
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="updateAction">更新操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateAsync(string taskId, Action<AIGenerateProgressDto> updateAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务进度
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RemoveAsync(string taskId, CancellationToken cancellationToken = default);
}
