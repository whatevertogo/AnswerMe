using AnswerMe.Domain.Entities;

namespace AnswerMe.Domain.Interfaces;

/// <summary>
/// 题库仓储接口
/// </summary>
public interface IQuestionBankRepository
{
    Task<QuestionBank?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<QuestionBank>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<List<QuestionBank>> GetPagedAsync(int userId, int pageSize, int? lastId, CancellationToken cancellationToken = default);
    Task<List<QuestionBank>> SearchAsync(int userId, string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken cancellationToken = default);
    Task AddAsync(QuestionBank questionBank, CancellationToken cancellationToken = default);
    Task UpdateAsync(QuestionBank questionBank, CancellationToken cancellationToken = default);
    Task DeleteAsync(QuestionBank questionBank, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
