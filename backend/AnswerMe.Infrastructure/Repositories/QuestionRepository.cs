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

    /// <summary>
    /// 转义 LIKE 模式中的特殊字符，防止 SQL 注入和通配符误匹配
    /// </summary>
    private static string EscapeLikePattern(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "";

        return "%" + pattern.Replace("%", "[%]")
                            .Replace("_", "[_]")
                            .Replace("[", "[[]");
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

    public async Task<Dictionary<int, int>> CountByQuestionBankIdsAsync(List<int> bankIds, CancellationToken cancellationToken = default)
    {
        if (bankIds == null || bankIds.Count == 0)
        {
            return new Dictionary<int, int>();
        }

        return await _context.Questions
            .Where(q => bankIds.Contains(q.QuestionBankId))
            .GroupBy(q => q.QuestionBankId)
            .Select(g => new { BankId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BankId, g => g.Count, cancellationToken);
    }

    public async Task<List<Question>> GetPagedFilteredAsync(
        int questionBankId,
        int pageSize,
        int? lastId,
        string? difficulty,
        string? questionType,
        CancellationToken cancellationToken = default)
    {
        // 构建基础查询
        var query = _context.Questions
            .Where(q => q.QuestionBankId == questionBankId);

        // 应用游标分页条件
        if (lastId.HasValue)
        {
            query = query.Where(q => q.Id < lastId.Value);
        }

        // 应用过滤条件
        if (!string.IsNullOrEmpty(difficulty))
        {
            query = query.Where(q => q.Difficulty == difficulty);
        }

        if (!string.IsNullOrEmpty(questionType))
        {
            query = query.Where(q => q.QuestionType == questionType);
        }

        // 最后执行排序和分页
        return await query
            .OrderByDescending(q => q.Id)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountFilteredAsync(
        int questionBankId,
        string? difficulty,
        string? questionType,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Where(q => q.QuestionBankId == questionBankId);

        if (!string.IsNullOrEmpty(difficulty))
        {
            query = query.Where(q => q.Difficulty == difficulty);
        }

        if (!string.IsNullOrEmpty(questionType))
        {
            query = query.Where(q => q.QuestionType == questionType);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<Question>> GetPagedAsync(int questionBankId, int pageSize, int? lastId, CancellationToken cancellationToken = default)
    {
        var query = _context.Questions
            .Where(q => q.QuestionBankId == questionBankId);

        if (lastId.HasValue)
        {
            query = query.Where(q => q.Id < lastId.Value);
        }

        return await query
            .OrderByDescending(q => q.Id)
            .Take(pageSize + 1) // 多取一个判断是否有更多
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Question>> SearchAsync(int questionBankId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        // 空搜索词时返回该题库所有题目
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await _context.Questions
                .Where(q => q.QuestionBankId == questionBankId)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync(cancellationToken);
        }

        var escapedPattern = EscapeLikePattern(searchTerm);

        return await _context.Questions
            .Where(q =>
                q.QuestionBankId == questionBankId &&
#pragma warning disable CS0618 // 旧字段兼容性代码：搜索时也检查旧字段
                (EF.Functions.Like(q.QuestionText, escapedPattern) ||
                 EF.Functions.Like(q.CorrectAnswer, escapedPattern) ||
#pragma warning restore CS0618
                 EF.Functions.Like(q.Explanation ?? "", escapedPattern)))
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
