using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Repositories;

/// <summary>
/// 答题详情仓储实现
/// </summary>
public class AttemptDetailRepository : IAttemptDetailRepository
{
    private readonly AnswerMeDbContext _context;

    public AttemptDetailRepository(AnswerMeDbContext context)
    {
        _context = context;
    }

    public async Task<AttemptDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.AttemptDetails
            .Include(d => d.Question)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<AttemptDetail?> GetByAttemptAndQuestionAsync(int attemptId, int questionId, CancellationToken cancellationToken = default)
    {
        return await _context.AttemptDetails
            .Include(d => d.Question)
            .FirstOrDefaultAsync(d => d.AttemptId == attemptId && d.QuestionId == questionId, cancellationToken);
    }

    public Task<AttemptDetail> AddAsync(AttemptDetail detail, CancellationToken cancellationToken = default)
    {
        _context.AttemptDetails.Add(detail);
        return Task.FromResult(detail);
    }

    public Task UpdateAsync(AttemptDetail detail, CancellationToken cancellationToken = default)
    {
        _context.AttemptDetails.Update(detail);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<AttemptDetail>> GetByAttemptIdAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        return await _context.AttemptDetails
            .Where(d => d.AttemptId == attemptId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttemptDetail>> GetByAttemptIdWithQuestionsAsync(int attemptId, CancellationToken cancellationToken = default)
    {
        return await _context.AttemptDetails
            .Where(d => d.AttemptId == attemptId)
            .Include(d => d.Question)
            .OrderBy(d => d.Id)
            .ToListAsync(cancellationToken);
    }
}
