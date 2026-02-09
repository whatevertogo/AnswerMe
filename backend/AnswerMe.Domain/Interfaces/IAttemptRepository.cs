using AnswerMe.Domain.Entities;

namespace AnswerMe.Domain.Interfaces;

/// <summary>
/// 答题记录仓储接口
/// </summary>
public interface IAttemptRepository
{
    Task<Attempt?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Attempt> AddAsync(Attempt attempt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Attempt attempt, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户在指定题库的答题记录
    /// </summary>
    Task<List<Attempt>> GetByQuestionBankIdAsync(int questionBankId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的所有答题记录
    /// </summary>
    Task<List<Attempt>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户正在进行的答题
    /// </summary>
    Task<Attempt?> GetActiveAttemptAsync(int userId, int questionBankId, CancellationToken cancellationToken = default);
}
