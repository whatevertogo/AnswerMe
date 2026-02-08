using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Repositories;

/// <summary>
/// 数据源仓储实现
/// </summary>
public class DataSourceRepository : IDataSourceRepository
{
    private readonly AnswerMeDbContext _context;

    public DataSourceRepository(AnswerMeDbContext context)
    {
        _context = context;
    }

    public async Task<DataSource?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DataSources
            .FirstOrDefaultAsync(ds => ds.Id == id, cancellationToken);
    }

    public async Task<List<DataSource>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.DataSources
            .Where(ds => ds.UserId == userId)
            .OrderByDescending(ds => ds.IsDefault)
            .ThenByDescending(ds => ds.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<DataSource?> GetDefaultByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.DataSources
            .FirstOrDefaultAsync(ds => ds.UserId == userId && ds.IsDefault, cancellationToken);
    }

    public async Task AddAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        await _context.DataSources.AddAsync(dataSource, cancellationToken);
    }

    public async Task UpdateAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _context.DataSources.Update(dataSource);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(DataSource dataSource, CancellationToken cancellationToken = default)
    {
        _context.DataSources.Remove(dataSource);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
