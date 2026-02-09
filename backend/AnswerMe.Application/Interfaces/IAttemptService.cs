using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 答题服务接口
/// </summary>
public interface IAttemptService
{
    /// <summary>
    /// 开始答题
    /// </summary>
    Task<StartAttemptResponseDto> StartAttemptAsync(int userId, StartAttemptDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交单个答案
    /// </summary>
    Task<bool> SubmitAnswerAsync(int userId, SubmitAnswerDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 完成答题
    /// </summary>
    Task<AttemptDto> CompleteAttemptAsync(int userId, CompleteAttemptDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取答题记录详情
    /// </summary>
    Task<AttemptDto?> GetAttemptByIdAsync(int attemptId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取答题记录的详情列表
    /// </summary>
    Task<List<AttemptDetailDto>> GetAttemptDetailsAsync(int attemptId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的答题统计
    /// </summary>
    Task<AttemptStatisticsDto> GetStatisticsAsync(int userId, CancellationToken cancellationToken = default);
}
