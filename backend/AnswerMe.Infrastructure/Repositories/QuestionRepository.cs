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

    public async Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default)
    {
        // 异步添加实体到上下文，避免同步阻塞
        await Task.Run(() => _context.Questions.Add(question), cancellationToken);
        return question;
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
