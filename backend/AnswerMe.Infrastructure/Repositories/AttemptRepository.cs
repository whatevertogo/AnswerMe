using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Repositories;

/// <summary>
/// 答题记录仓储实现
/// </summary>
public class AttemptRepository : IAttemptRepository
{
    private readonly AnswerMeDbContext _context;

    public AttemptRepository(AnswerMeDbContext context)
    {
        _context = context;
    }

    public async Task<Attempt?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Attempts
            .Include(a => a.AttemptDetails)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<Attempt> AddAsync(Attempt attempt, CancellationToken cancellationToken = default)
    {
        _context.Attempts.Add(attempt);
        return Task.FromResult(attempt);
    }

    public Task UpdateAsync(Attempt attempt, CancellationToken cancellationToken = default)
    {
        _context.Attempts.Update(attempt);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var attempt = await GetByIdAsync(id, cancellationToken);
        if (attempt != null)
        {
            _context.Attempts.Remove(attempt);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Attempt>> GetByQuestionBankIdAsync(int questionBankId, int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Attempts
            .Where(a => a.QuestionBankId == questionBankId && a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .Include(a => a.QuestionBank)
            .Include(a => a.AttemptDetails)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Attempt>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Attempts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.StartedAt)
            .Include(a => a.QuestionBank)
            .Include(a => a.AttemptDetails)
            .ToListAsync(cancellationToken);
    }

    public async Task<Attempt?> GetActiveAttemptAsync(int userId, int questionBankId, CancellationToken cancellationToken = default)
    {
        return await _context.Attempts
            .Where(a => a.UserId == userId && a.QuestionBankId == questionBankId && a.CompletedAt == null)
            .Include(a => a.AttemptDetails)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CountByUserIdAndDateRangeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Attempts
            .CountAsync(a => a.UserId == userId && a.StartedAt >= startDate && a.StartedAt <= endDate, cancellationToken);
    }
}
