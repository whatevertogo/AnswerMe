using AnswerMe.Application.Common;
using AnswerMe.Domain.Common;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Repositories;

/// <summary>
/// 题库仓储实现
/// </summary>
public class QuestionBankRepository : IQuestionBankRepository
{
    private readonly AnswerMeDbContext _context;

    public QuestionBankRepository(AnswerMeDbContext context)
    {
        _context = context;
    }

    public async Task<QuestionBank?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionBanks
            .Include(qb => qb.DataSource)
            .FirstOrDefaultAsync(qb => qb.Id == id, cancellationToken);
    }

    public async Task<List<QuestionBank>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionBanks
            .Include(qb => qb.DataSource)
            .Where(qb => qb.UserId == userId)
            .OrderByDescending(qb => qb.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<QuestionBank>> GetPagedAsync(int userId, int pageSize, int? lastId, CancellationToken cancellationToken = default)
    {
        var query = _context.QuestionBanks
            .Include(qb => qb.DataSource)
            .Where(qb => qb.UserId == userId);

        if (lastId.HasValue)
        {
            query = query.Where(qb => qb.Id < lastId.Value);
        }

        return await query
            .OrderByDescending(qb => qb.Id)
            .Take(pageSize + 1) // 多取一个判断是否有更多
            .ToListAsync(cancellationToken);
    }

    public async Task<List<QuestionBank>> SearchAsync(int userId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var escapedPattern = LikePatternHelper.EscapeLikePattern(searchTerm);

        return await _context.QuestionBanks
            .Include(qb => qb.DataSource)
            .Where(qb =>
                qb.UserId == userId &&
                (EF.Functions.Like(qb.Name, escapedPattern) ||
                 EF.Functions.Like(qb.Description ?? "", escapedPattern)))
            .OrderByDescending(qb => qb.UpdatedAt)
            .Take(BatchLimits.MaxSearchResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(int userId, string name, CancellationToken cancellationToken = default)
    {
        return await _context.QuestionBanks
            .AnyAsync(qb => qb.UserId == userId && qb.Name == name, cancellationToken);
    }

    public async Task AddAsync(QuestionBank questionBank, CancellationToken cancellationToken = default)
    {
        await _context.QuestionBanks.AddAsync(questionBank, cancellationToken);
    }

    public async Task UpdateAsync(QuestionBank questionBank, CancellationToken cancellationToken = default)
    {
        _context.QuestionBanks.Update(questionBank);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(QuestionBank questionBank, CancellationToken cancellationToken = default)
    {
        _context.QuestionBanks.Remove(questionBank);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<QuestionBank>> GetByIdsAndUserIdAsync(List<int> ids, int userId, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
        {
            return new List<QuestionBank>();
        }

        return await _context.QuestionBanks
            .Where(qb => ids.Contains(qb.Id) && qb.UserId == userId)
            .ToListAsync(cancellationToken);
    }
}
