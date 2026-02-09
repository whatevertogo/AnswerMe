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
}
