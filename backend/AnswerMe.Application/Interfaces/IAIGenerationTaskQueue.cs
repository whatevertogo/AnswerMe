using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// AI 生成任务队列接口
/// </summary>
public interface IAIGenerationTaskQueue
{
    /// <summary>
    /// 将任务加入队列
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="request">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnqueueAsync(string taskId, int userId, AIGenerateRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 从队列中取出一个任务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务信息元组(taskId, userId, request)，如果没有任务返回 null</returns>
    Task<(string taskId, int userId, AIGenerateRequestDto request)?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记任务完成，清理任务数据
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task CompleteTaskAsync(string taskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取队列长度
    /// </summary>
    Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default);
}
