using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Infrastructure.Repositories;

/// <summary>
/// 题目仓储实现
/// </summary>
public class QuestionRepository : IQuestionRepository
{
    private readonly AnswerMeDbContext _context;

    public QuestionRepository(AnswerMeDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Include(q => q.QuestionBank)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
    }

    public async Task<List<Question>> GetByQuestionBankIdAsync(int questionBankId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Where(q => q.QuestionBankId == questionBankId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByQuestionBankIdAsync(int questionBankId, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .CountAsync(q => q.QuestionBankId == questionBankId, cancellationToken);
    }

    public async Task<List<Question>> GetPagedAsync(int questionBankId, int pageSize, int? lastId, CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Where(q => q.QuestionBankId == questionBankId)
            .OrderByDescending(q => q.Id)
            .Take(pageSize + 1); // 多取一个判断是否有更多

        if (lastId.HasValue)
        {
            query = query.Where(q => q.Id < lastId.Value);
        }

        var results = await query.ToListAsync(cancellationToken);
        return results.Take(pageSize).ToList();
    }

    public async Task<List<Question>> SearchAsync(int questionBankId, string searchTerm, CancellationToken cancellationToken = default)
    {
        return await _context.Questions
            .Where(q =>
                q.QuestionBankId == questionBankId &&
                (EF.Functions.Like(q.QuestionText, $"%{searchTerm}%") ||
                 EF.Functions.Like(q.CorrectAnswer, $"%{searchTerm}%") ||
                 EF.Functions.Like(q.Explanation ?? "", $"%{searchTerm}%")))
            .OrderBy(q => q.OrderIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        await _context.Questions.AddAsync(question, cancellationToken);
        return question;
    }

    public async Task AddRangeAsync(List<Question> questions, CancellationToken cancellationToken = default)
    {
        await _context.Questions.AddRangeAsync(questions, cancellationToken);
    }

    public async Task<Question> UpdateAsync(Question question, CancellationToken cancellationToken = default)
    {
        _context.Questions.Update(question);
        await _context.SaveChangesAsync(cancellationToken);
        return question;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (question == null)
        {
            return false;
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
