using AnswerMe.Domain.Entities;

namespace AnswerMe.Domain.Interfaces;

/// <summary>
/// 数据源仓储接口
/// </summary>
public interface IDataSourceRepository
{
    Task<DataSource?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<DataSource>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<DataSource?> GetDefaultByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task AddAsync(DataSource dataSource, CancellationToken cancellationToken = default);
    Task UpdateAsync(DataSource dataSource, CancellationToken cancellationToken = default);
    Task DeleteAsync(DataSource dataSource, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
