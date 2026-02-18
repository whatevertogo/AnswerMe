using AnswerMe.Application.Common;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Common;
using System.Text.Json;
using System.Linq;

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
        if (!await HasQuestionBankAccessAsync(dto.QuestionBankId, userId, cancellationToken))
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        var question = new Domain.Entities.Question
        {
            QuestionBankId = dto.QuestionBankId,
            QuestionText = dto.QuestionText,
            QuestionTypeEnum = ResolveQuestionType(dto.QuestionTypeEnum, dto.Data),
            Explanation = dto.Explanation,
            Difficulty = dto.Difficulty,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var data = NormalizeQuestionData(
            dto.Data,
            question.QuestionTypeEnum,
            dto.Options,
            dto.CorrectAnswer,
            dto.Explanation,
            dto.Difficulty);

        ApplyQuestionData(question, data);

        await _questionRepository.AddAsync(question, cancellationToken);
        await _questionRepository.SaveChangesAsync(cancellationToken);

        return question.ToDto();
    }

    public async Task<QuestionListDto> GetListAsync(int userId, QuestionListQueryDto query, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        if (!await HasQuestionBankAccessAsync(query.QuestionBankId, userId, cancellationToken))
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        // 如果有搜索关键词,使用搜索
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var questions = await _questionRepository.SearchAsync(query.QuestionBankId, query.Search, cancellationToken);
            var typeFilter = query.QuestionTypeEnum?.ToString() ?? query.QuestionType;
            var result = questions
                .Where(q =>
                    (string.IsNullOrEmpty(query.Difficulty) || q.Difficulty == query.Difficulty) &&
                    (string.IsNullOrEmpty(typeFilter) || q.QuestionType == typeFilter))
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

        // 将过滤条件下推到数据库
        var typeFilterPaged = query.QuestionTypeEnum?.ToString() ?? query.QuestionType;
        var questionsPaged = await _questionRepository.GetPagedFilteredAsync(
            query.QuestionBankId,
            query.PageSize + 1,
            query.LastId,
            query.Difficulty,
            typeFilterPaged,
            cancellationToken);

        // 获取符合过滤条件的总数
        var totalCount = await _questionRepository.CountFilteredAsync(
            query.QuestionBankId,
            query.Difficulty,
            typeFilterPaged,
            cancellationToken);

        var hasMore = questionsPaged.Count > query.PageSize;
        var pageItems = hasMore ? questionsPaged.Take(query.PageSize).ToList() : questionsPaged;
        var resultList = pageItems.ToDtoList();
        int? nextCursor = hasMore ? pageItems.LastOrDefault()?.Id : (int?)null;

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
        if (!await HasQuestionAccessAsync(question, userId, cancellationToken))
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
        if (!await HasQuestionAccessAsync(question, userId, cancellationToken))
        {
            return null;
        }

        // 更新字段
        if (dto.QuestionText != null)
        {
            question.QuestionText = dto.QuestionText;
        }

        var resolvedType = ResolveQuestionType(dto.QuestionTypeEnum ?? question.QuestionTypeEnum, dto.Data);
        if (resolvedType.HasValue)
        {
            question.QuestionTypeEnum = resolvedType;
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

        var updatedData = NormalizeQuestionData(
            dto.Data,
            question.QuestionTypeEnum,
            dto.Options,
            dto.CorrectAnswer,
            dto.Explanation ?? question.Explanation,
            dto.Difficulty ?? question.Difficulty);

        if (updatedData != null)
        {
            ApplyQuestionData(question, updatedData);
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
        if (!await HasQuestionAccessAsync(question, userId, cancellationToken))
        {
            return false;
        }

        await _questionRepository.DeleteAsync(id, cancellationToken);
        return true;
    }

    public async Task<List<QuestionDto>> SearchAsync(int questionBankId, int userId, string searchTerm, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        if (!await HasQuestionBankAccessAsync(questionBankId, userId, cancellationToken))
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
        var userQuestionBanks = await _questionBankRepository.GetByUserIdAsync(userId, cancellationToken);
        var userQuestionBankIds = userQuestionBanks.Select(qb => qb.Id).ToHashSet();

        foreach (var questionBankId in questionBankIds)
        {
            if (!userQuestionBankIds.Contains(questionBankId))
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
                QuestionTypeEnum = ResolveQuestionType(dto.QuestionTypeEnum, dto.Data),
                Explanation = dto.Explanation,
                Difficulty = dto.Difficulty,
                OrderIndex = dto.OrderIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var data = NormalizeQuestionData(
                dto.Data,
                question.QuestionTypeEnum,
                dto.Options,
                dto.CorrectAnswer,
                dto.Explanation,
                dto.Difficulty);

            ApplyQuestionData(question, data);
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

        // 批量获取所有题目及其题库信息
        var questions = await _questionRepository.GetByIdsAsync(ids, cancellationToken);

        if (questions.Count == 0)
        {
            return (0, ids.Count);
        }

        // 获取所有涉及的题库ID
        var questionBankIds = questions.Select(q => q.QuestionBankId).Distinct().ToList();

        // 批量验证用户权限
        var userQuestionBanks = await _questionBankRepository.GetByIdsAndUserIdAsync(questionBankIds, userId, cancellationToken);
        var authorizedBankIds = userQuestionBanks.Select(qb => qb.Id).ToHashSet();

        // 筛选有权限删除的题目ID
        var authorizedIds = questions
            .Where(q => authorizedBankIds.Contains(q.QuestionBankId))
            .Select(q => q.Id)
            .ToList();

        // 批量删除
        var deletedCount = authorizedIds.Count > 0
            ? await _questionRepository.DeleteRangeAsync(authorizedIds, cancellationToken)
            : 0;

        var notFoundCount = ids.Count - deletedCount;

        return (deletedCount, notFoundCount);
    }

    private static QuestionType? ResolveQuestionType(QuestionType? requestedType, QuestionData? data)
    {
        if (requestedType.HasValue)
        {
            return requestedType;
        }

        if (data is ChoiceQuestionData choiceData)
        {
            return choiceData.CorrectAnswers.Count > 1
                ? QuestionType.MultipleChoice
                : QuestionType.SingleChoice;
        }

        return data switch
        {
            BooleanQuestionData => QuestionType.TrueFalse,
            FillBlankQuestionData => QuestionType.FillBlank,
            ShortAnswerQuestionData => QuestionType.ShortAnswer,
            _ => null
        };
    }

    private static QuestionData? NormalizeQuestionData(
        QuestionData? data,
        QuestionType? questionType,
#pragma warning disable CS0618 // 旧字段兼容性代码：DTO 中的 Options/CorrectAnswer 用于向后兼容
        string? legacyOptions,
        string? legacyCorrectAnswer,
#pragma warning restore CS0618
        string? explanation,
        string? difficulty)
    {
        if (data == null)
        {
            data = BuildDataFromLegacy(questionType, legacyOptions, legacyCorrectAnswer, explanation, difficulty);
        }

        if (data == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(explanation) && string.IsNullOrWhiteSpace(data.Explanation))
        {
            data.Explanation = explanation;
        }

        if (!string.IsNullOrWhiteSpace(difficulty))
        {
            data.Difficulty = difficulty;
        }

        return data;
    }

    /// <summary>
    /// 应用题目数据到实体
    /// 只更新新字段（QuestionDataJson），不再同步更新旧字段
    /// 旧字段仅用于历史数据兼容
    /// </summary>
    private static void ApplyQuestionData(Domain.Entities.Question question, QuestionData? data)
    {
        if (data == null)
        {
            return;
        }

        // 只更新新字段
        question.Data = data;

        // 将 Explanation 和 Difficulty 提取到顶层字段（便于数据库查询）
        if (!string.IsNullOrWhiteSpace(data.Explanation))
        {
            question.Explanation = data.Explanation;
        }

        if (!string.IsNullOrWhiteSpace(data.Difficulty))
        {
            question.Difficulty = data.Difficulty;
        }

        // 不再更新旧字段（Options、CorrectAnswer）
        // 历史数据的旧字段保留用于读取兼容，新数据不写入旧字段
    }

    private static QuestionData? BuildDataFromLegacy(
        QuestionType? questionType,
        string? legacyOptions,
        string? legacyCorrectAnswer,
        string? explanation,
        string? difficulty)
    {
        if (questionType == null)
        {
            return null;
        }

        switch (questionType)
        {
            case QuestionType.SingleChoice:
            case QuestionType.MultipleChoice:
            {
                var options = ParseLegacyOptions(legacyOptions);
                var correctAnswers = ParseLegacyAnswers(legacyCorrectAnswer);
                if (options.Count == 0 && correctAnswers.Count == 0 && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new ChoiceQuestionData
                {
                    Options = options,
                    CorrectAnswers = correctAnswers,
                    Explanation = explanation,
                    Difficulty = difficulty ?? "medium"
                };
            }
            case QuestionType.TrueFalse:
            {
                var booleanAnswer = LegacyFieldParser.ParseBooleanAnswer(legacyCorrectAnswer);
                if (!booleanAnswer.HasValue)
                {
                    return null;
                }
                return new BooleanQuestionData
                {
                    CorrectAnswer = booleanAnswer.Value,
                    Explanation = explanation,
                    Difficulty = difficulty ?? "medium"
                };
            }
            case QuestionType.FillBlank:
            {
                var answers = ParseLegacyAnswers(legacyCorrectAnswer);
                if (answers.Count == 0 && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new FillBlankQuestionData
                {
                    AcceptableAnswers = answers,
                    Explanation = explanation,
                    Difficulty = difficulty ?? "medium"
                };
            }
            case QuestionType.ShortAnswer:
            {
                if (string.IsNullOrWhiteSpace(legacyCorrectAnswer) && string.IsNullOrWhiteSpace(explanation))
                {
                    return null;
                }
                return new ShortAnswerQuestionData
                {
                    ReferenceAnswer = legacyCorrectAnswer ?? string.Empty,
                    Explanation = explanation,
                    Difficulty = difficulty ?? "medium"
                };
            }
            default:
                return null;
        }
    }

    private static List<string> ParseLegacyOptions(string? legacyOptions)
    {
        return LegacyFieldParser.ParseDelimitedList(legacyOptions);
    }

    private static List<string> ParseLegacyAnswers(string? legacyAnswers)
    {
        return LegacyFieldParser.ParseCorrectAnswers(legacyAnswers);
    }

    private async Task<bool> HasQuestionBankAccessAsync(
        int questionBankId,
        int userId,
        CancellationToken cancellationToken)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(questionBankId, cancellationToken);
        return questionBank != null && questionBank.UserId == userId;
    }

    private async Task<bool> HasQuestionAccessAsync(
        Domain.Entities.Question question,
        int userId,
        CancellationToken cancellationToken)
    {
        if (question.QuestionBank != null)
        {
            return question.QuestionBank.UserId == userId;
        }

        return await HasQuestionBankAccessAsync(question.QuestionBankId, userId, cancellationToken);
    }
}
