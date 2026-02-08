using AnswerMe.Domain.Entities;

namespace AnswerMe.Domain.Interfaces;

/// <summary>
/// 题目仓储接口
/// </summary>
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<Question>> GetByQuestionBankIdAsync(int questionBankId, CancellationToken cancellationToken = default);
    Task<int> CountByQuestionBankIdAsync(int questionBankId, CancellationToken cancellationToken = default);
    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
    Task<Question> UpdateAsync(Question question, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
