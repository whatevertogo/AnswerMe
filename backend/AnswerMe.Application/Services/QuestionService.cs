using AnswerMe.Application.Common;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Services;

/// <summary>
/// 题目服务实现
/// </summary>
public class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionBankRepository _questionBankRepository;

    public QuestionService(
        IQuestionRepository questionRepository,
        IQuestionBankRepository questionBankRepository)
    {
        _questionRepository = questionRepository;
        _questionBankRepository = questionBankRepository;
    }

    public async Task<QuestionDto> CreateAsync(int userId, CreateQuestionDto dto, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(dto.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        var question = new Domain.Entities.Question
        {
            QuestionBankId = dto.QuestionBankId,
            QuestionText = dto.QuestionText,
            QuestionType = dto.QuestionType,
            Options = dto.Options,
            CorrectAnswer = dto.CorrectAnswer,
            Explanation = dto.Explanation,
            Difficulty = dto.Difficulty,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _questionRepository.AddAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        return question.ToDto();
    }

    public async Task<QuestionListDto> GetListAsync(int userId, QuestionListQueryDto query, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(query.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        // 如果有搜索关键词,使用搜索
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var questions = await _questionRepository.SearchAsync(query.QuestionBankId, query.Search, cancellationToken);
            var result = questions
                .Where(q =>
                    (string.IsNullOrEmpty(query.Difficulty) || q.Difficulty == query.Difficulty) &&
                    (string.IsNullOrEmpty(query.QuestionType) || q.QuestionType == query.QuestionType))
                .ToList();

            var dtos = result.ToDtoList();

            return new QuestionListDto
            {
                Data = dtos,
                HasMore = false,
                NextCursor = null,
                TotalCount = dtos.Count
            };
        }

        var questionsPaged = await _questionRepository.GetPagedAsync(
            query.QuestionBankId,
            query.PageSize,
            query.LastId,
            cancellationToken);

        // 根据难度和类型过滤
        var filteredQuestions = questionsPaged
            .Where(q =>
                (string.IsNullOrEmpty(query.Difficulty) || q.Difficulty == query.Difficulty) &&
                (string.IsNullOrEmpty(query.QuestionType) || q.QuestionType == query.QuestionType))
            .ToList();

        var resultList = filteredQuestions.ToDtoList();

        // 获取总数
        var totalCount = await _questionRepository.CountByQuestionBankIdAsync(query.QuestionBankId, cancellationToken);

        // 判断是否有更多数据
        var hasMore = filteredQuestions.Count == query.PageSize;
        int? nextCursor = hasMore ? filteredQuestions.LastOrDefault()?.Id : (int?)null;

        return new QuestionListDto
        {
            Data = resultList,
            HasMore = hasMore,
            NextCursor = nextCursor,
            TotalCount = totalCount
        };
    }

    public async Task<QuestionDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
        if (question == null)
        {
            return null;
        }

        // 验证题库是否属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(question.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return null;
        }

        return question.ToDto();
    }

    public async Task<QuestionDto?> UpdateAsync(int id, int userId, UpdateQuestionDto dto, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
        if (question == null)
        {
            return null;
        }

        // 验证题库是否属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(question.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return null;
        }

        // 更新字段
        if (dto.QuestionText != null)
        {
            question.QuestionText = dto.QuestionText;
        }

        if (dto.QuestionType != null)
        {
            question.QuestionType = dto.QuestionType;
        }

        if (dto.Options != null)
        {
            question.Options = dto.Options;
        }

        if (dto.CorrectAnswer != null)
        {
            question.CorrectAnswer = dto.CorrectAnswer;
        }

        if (dto.Explanation != null)
        {
            question.Explanation = dto.Explanation;
        }

        if (dto.Difficulty != null)
        {
            question.Difficulty = dto.Difficulty;
        }

        if (dto.OrderIndex.HasValue)
        {
            question.OrderIndex = dto.OrderIndex.Value;
        }

        question.UpdatedAt = DateTime.UtcNow;

        await _questionRepository.SaveChangesAsync(cancellationToken);

        return question.ToDto();
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
        if (question == null)
        {
            return false;
        }

        // 验证题库是否属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(question.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return false;
        }

        await _questionRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<List<QuestionDto>> SearchAsync(int questionBankId, int userId, string searchTerm, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(questionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        var questions = await _questionRepository.SearchAsync(questionBankId, searchTerm, cancellationToken);
        return questions.ToDtoList();
    }

    public async Task<List<QuestionDto>> CreateBatchAsync(int userId, List<CreateQuestionDto> dtos, CancellationToken cancellationToken = default)
    {
        if (dtos == null || dtos.Count == 0)
        {
            throw new InvalidOperationException("创建题目列表不能为空");
        }

        // 验证所有题库是否存在且属于当前用户
        var questionBankIds = dtos.Select(d => d.QuestionBankId).Distinct().ToList();
        foreach (var questionBankId in questionBankIds)
        {
            var questionBank = await _questionBankRepository.GetByIdAsync(questionBankId, cancellationToken);
            if (questionBank == null || questionBank.UserId != userId)
            {
                throw new InvalidOperationException($"题库 {questionBankId} 不存在或无权访问");
            }
        }

        var questions = new List<Domain.Entities.Question>();
        foreach (var dto in dtos)
        {
            var question = new Domain.Entities.Question
            {
                QuestionBankId = dto.QuestionBankId,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                Options = dto.Options,
                CorrectAnswer = dto.CorrectAnswer,
                Explanation = dto.Explanation,
                Difficulty = dto.Difficulty,
                OrderIndex = dto.OrderIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            questions.Add(question);
        }

        await _questionRepository.AddRangeAsync(questions, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        return questions.ToDtoList();
    }

    public async Task<(int successCount, int notFoundCount)> DeleteBatchAsync(int userId, List<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || ids.Count == 0)
        {
            return (0, 0);
        }

        var successCount = 0;
        var notFoundCount = 0;

        foreach (var id in ids)
        {
            var question = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (question == null)
            {
                notFoundCount++;
                continue;
            }

            // 验证题库是否属于当前用户
            var questionBank = await _questionBankRepository.GetByIdAsync(question.QuestionBankId, cancellationToken);
            if (questionBank == null || questionBank.UserId != userId)
            {
                notFoundCount++;
                continue;
            }

            await _questionRepository.DeleteAsync(id, cancellationToken);
            successCount++;
        }

        return (successCount, notFoundCount);
    }
}
