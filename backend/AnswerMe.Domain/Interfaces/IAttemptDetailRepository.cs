using AnswerMe.Domain.Entities;

namespace AnswerMe.Domain.Interfaces;

/// <summary>
/// 答题详情仓储接口
/// </summary>
public interface IAttemptDetailRepository
{
    Task<AttemptDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AttemptDetail?> GetByAttemptAndQuestionAsync(int attemptId, int questionId, CancellationToken cancellationToken = default);
    Task<AttemptDetail> AddAsync(AttemptDetail detail, CancellationToken cancellationToken = default);
    Task UpdateAsync(AttemptDetail detail, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<AttemptDetail>> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取答题详情的所有详情(带题目信息)
    /// </summary>
    Task<List<AttemptDetail>> GetByAttemptIdWithQuestionsAsync(int attemptId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取多个答题记录的详情（优化 N+1 查询）
    /// </summary>
    /// <param name="attemptIds">答题记录ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<List<AttemptDetail>> GetByAttemptIdsAsync(List<int> attemptIds, CancellationToken cancellationToken = default);
}
