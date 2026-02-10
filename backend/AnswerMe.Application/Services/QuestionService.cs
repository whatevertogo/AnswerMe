using AnswerMe.Application.Common;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
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
        var questionBank = await _questionBankRepository.GetByIdAsync(dto.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
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
        var questionBank = await _questionBankRepository.GetByIdAsync(query.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
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

        var questionsPaged = await _questionRepository.GetPagedAsync(
            query.QuestionBankId,
            query.PageSize,
            query.LastId,
            cancellationToken);

        // 根据难度和类型过滤
        var typeFilterPaged = query.QuestionTypeEnum?.ToString() ?? query.QuestionType;
        var filteredQuestions = questionsPaged
            .Where(q =>
                (string.IsNullOrEmpty(query.Difficulty) || q.Difficulty == query.Difficulty) &&
                (string.IsNullOrEmpty(typeFilterPaged) || q.QuestionType == typeFilterPaged))
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
        string? legacyOptions,
        string? legacyCorrectAnswer,
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

    private static void ApplyQuestionData(Domain.Entities.Question question, QuestionData? data)
    {
        if (data == null)
        {
            return;
        }

        question.Data = data;

        if (!string.IsNullOrWhiteSpace(data.Explanation))
        {
            question.Explanation = data.Explanation;
        }

        if (!string.IsNullOrWhiteSpace(data.Difficulty))
        {
            question.Difficulty = data.Difficulty;
        }

        switch (data)
        {
            case ChoiceQuestionData choiceData:
                question.Options = JsonSerializer.Serialize(choiceData.Options);
                question.CorrectAnswer = string.Join(",", choiceData.CorrectAnswers);
                break;
            case BooleanQuestionData booleanData:
                question.Options = null;
                question.CorrectAnswer = booleanData.CorrectAnswer ? "true" : "false";
                break;
            case FillBlankQuestionData fillBlankData:
                question.Options = null;
                question.CorrectAnswer = string.Join(",", fillBlankData.AcceptableAnswers);
                break;
            case ShortAnswerQuestionData shortAnswerData:
                question.Options = null;
                question.CorrectAnswer = shortAnswerData.ReferenceAnswer;
                break;
        }
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
                if (!bool.TryParse(legacyCorrectAnswer, out var booleanAnswer))
                {
                    return null;
                }
                return new BooleanQuestionData
                {
                    CorrectAnswer = booleanAnswer,
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
        if (string.IsNullOrWhiteSpace(legacyOptions))
        {
            return new List<string>();
        }

        var trimmed = legacyOptions.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(legacyOptions) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static List<string> ParseLegacyAnswers(string? legacyAnswers)
    {
        if (string.IsNullOrWhiteSpace(legacyAnswers))
        {
            return new List<string>();
        }

        var trimmed = legacyAnswers.Trim();
        if (trimmed.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(legacyAnswers) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }
}
