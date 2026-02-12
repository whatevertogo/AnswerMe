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

    public async Task<List<AttemptDetail>> GetByAttemptIdsAsync(List<int> attemptIds, CancellationToken cancellationToken = default)
    {
        if (attemptIds == null || attemptIds.Count == 0)
        {
            return new List<AttemptDetail>();
        }

        return await _context.AttemptDetails
            .Where(d => attemptIds.Contains(d.AttemptId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AttemptDetail>> GetWrongQuestionsAsync(
        int userId,
        int? questionBankId = null,
        string? questionType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AttemptDetails
            .Include(d => d.Question)
            .Include(d => d.Attempt)
                .ThenInclude(a => a!.QuestionBank)
            .Where(d => d.Attempt!.UserId == userId && d.IsCorrect == false)
            .AsQueryable();

        if (questionBankId.HasValue)
        {
            query = query.Where(d => d.Attempt!.QuestionBankId == questionBankId.Value);
        }

        if (!string.IsNullOrEmpty(questionType))
        {
            query = query.Where(d => d.Question!.QuestionType == questionType);
        }

        if (startDate.HasValue)
        {
            query = query.Where(d => d.Attempt!.StartedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(d => d.Attempt!.StartedAt <= endDate.Value);
        }

        return await query
            .OrderByDescending(d => d.Attempt!.StartedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int totalAttempts, int totalQuestions, int correctCount, int wrongCount, int totalTimeSpent)> GetLearningStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var attempts = await _context.Attempts
            .Where(a => a.UserId == userId && a.CompletedAt != null)
            .ToListAsync(cancellationToken);

        var details = await _context.AttemptDetails
            .Include(d => d.Attempt)
            .Where(d => d.Attempt!.UserId == userId && d.Attempt!.CompletedAt != null)
            .ToListAsync(cancellationToken);

        var totalAttempts = attempts.Count;
        var totalQuestions = details.Count;
        var correctCount = details.Count(d => d.IsCorrect == true);
        var wrongCount = details.Count(d => d.IsCorrect == false);
        var totalTimeSpent = details.Sum(d => d.TimeSpent ?? 0);

        return (totalAttempts, totalQuestions, correctCount, wrongCount, totalTimeSpent);
    }

    public async Task<List<(DateTime weekStart, int attemptCount, int questionCount, int correctCount)>> GetWeeklyTrendAsync(
        int userId,
        int weeks = 12,
        CancellationToken cancellationToken = default)
    {
        var startDate = DateTime.UtcNow.AddDays(-weeks * 7);

        var attempts = await _context.Attempts
            .Where(a => a.UserId == userId && a.CompletedAt != null && a.StartedAt >= startDate)
            .ToListAsync(cancellationToken);

        var attemptIds = attempts.Select(a => a.Id).ToList();

        var details = await _context.AttemptDetails
            .Where(d => attemptIds.Contains(d.AttemptId))
            .ToListAsync(cancellationToken);

        var result = attempts
            .GroupBy(a => GetWeekStart(a.StartedAt))
            .Select(g => (
                weekStart: g.Key,
                attemptCount: g.Count(),
                questionCount: details.Count(d => d.AttemptId != null && g.Select(a => a.Id).Contains(d.AttemptId)),
                correctCount: details.Count(d => d.AttemptId != null && g.Select(a => a.Id).Contains(d.AttemptId) && d.IsCorrect == true)
            ))
            .OrderBy(x => x.weekStart)
            .ToList();

        return result;
    }

    public async Task<List<(int questionBankId, string questionBankName, int attemptCount, int totalQuestions, int correctCount)>> GetBankStatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.Attempts
            .Include(a => a.QuestionBank)
            .Where(a => a.UserId == userId && a.CompletedAt != null)
            .GroupBy(a => new { a.QuestionBankId, a.QuestionBank!.Name })
            .Select(g => new
            {
                g.Key.QuestionBankId,
                g.Key.Name,
                AttemptCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var stats = new List<(int questionBankId, string questionBankName, int attemptCount, int totalQuestions, int correctCount)>();

        foreach (var r in result)
        {
            var details = await _context.AttemptDetails
                .Include(d => d.Attempt)
                .Where(d => d.Attempt!.UserId == userId && d.Attempt!.QuestionBankId == r.QuestionBankId && d.Attempt!.CompletedAt != null)
                .ToListAsync(cancellationToken);

            stats.Add((
                r.QuestionBankId,
                r.Name,
                r.AttemptCount,
                details.Count,
                details.Count(d => d.IsCorrect == true)
            ));
        }

        return stats;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
