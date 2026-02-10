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

    /// <summary>
    /// 批量统计多个题库的题目数量（优化 N+1 查询）
    /// </summary>
    /// <param name="bankIds">题库ID列表</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>题库ID到题目数量的映射字典</returns>
    Task<Dictionary<int, int>> CountByQuestionBankIdsAsync(List<int> bankIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取过滤后的分页题目列表（将过滤下推到数据库）
    /// </summary>
    Task<List<Question>> GetPagedFilteredAsync(
        int questionBankId,
        int pageSize,
        int? lastId,
        string? difficulty,
        string? questionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计符合过滤条件的题目数量
    /// </summary>
    Task<int> CountFilteredAsync(
        int questionBankId,
        string? difficulty,
        string? questionType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取题目列表(游标分页)
    /// </summary>
    Task<List<Question>> GetPagedAsync(int questionBankId, int pageSize, int? lastId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索题目
    /// </summary>
    Task<List<Question>> SearchAsync(int questionBankId, string searchTerm, CancellationToken cancellationToken = default);

    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
    Task AddRangeAsync(List<Question> questions, CancellationToken cancellationToken = default);
    Task<Question> UpdateAsync(Question question, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
