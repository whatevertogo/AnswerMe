using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Models;

namespace AnswerMe.Application.Services;

/// <summary>
/// 答题服务实现
/// </summary>
public class AttemptService : IAttemptService
{
    private readonly IAttemptRepository _attemptRepository;
    private readonly IAttemptDetailRepository _attemptDetailRepository;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionRepository _questionRepository;

    public AttemptService(
        IAttemptRepository attemptRepository,
        IAttemptDetailRepository attemptDetailRepository,
        IQuestionBankRepository questionBankRepository,
        IQuestionRepository questionRepository)
    {
        _attemptRepository = attemptRepository;
        _attemptDetailRepository = attemptDetailRepository;
        _questionBankRepository = questionBankRepository;
        _questionRepository = questionRepository;
    }

    public async Task<StartAttemptResponseDto> StartAttemptAsync(int userId, StartAttemptDto dto, CancellationToken cancellationToken = default)
    {
        // 验证题库是否存在且属于当前用户
        var questionBank = await _questionBankRepository.GetByIdAsync(dto.QuestionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }

        // 获取题库的所有题目
        var questions = await _questionRepository.GetByQuestionBankIdAsync(dto.QuestionBankId, cancellationToken);
        if (questions.Count == 0)
        {
            throw new InvalidOperationException("题库中没有题目");
        }

        // 根据模式决定题目顺序
        List<int> questionIds;
        if (dto.Mode == "random")
        {
            var random = new Random();
            questionIds = questions.OrderBy(_ => random.Next()).Select(q => q.Id).ToList();
        }
        else // sequential
        {
            questionIds = questions.OrderBy(q => q.OrderIndex).ThenBy(q => q.Id).Select(q => q.Id).ToList();
        }

        // 创建答题记录
        var attempt = new Attempt
        {
            UserId = userId,
            QuestionBankId = dto.QuestionBankId,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = questionIds.Count
        };

        await _attemptRepository.AddAsync(attempt, cancellationToken);
        await _attemptRepository.SaveChangesAsync(cancellationToken);

        return new StartAttemptResponseDto
        {
            AttemptId = attempt.Id,
            QuestionIds = questionIds
        };
    }

    public async Task<bool> SubmitAnswerAsync(int userId, SubmitAnswerDto dto, CancellationToken cancellationToken = default)
    {
        // 获取答题记录
        var attempt = await _attemptRepository.GetByIdAsync(dto.AttemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            throw new InvalidOperationException("答题记录不存在或无权访问");
        }

        if (attempt.CompletedAt != null)
        {
            throw new InvalidOperationException("答题已完成,无法提交答案");
        }

        // 获取题目
        var question = await _questionRepository.GetByIdAsync(dto.QuestionId, cancellationToken);
        if (question == null)
        {
            throw new InvalidOperationException("题目不存在");
        }

        // 检查是否已提交过答案(防止重复)
        var existingDetail = await _attemptDetailRepository.GetByAttemptAndQuestionAsync(dto.AttemptId, dto.QuestionId, cancellationToken);
        if (existingDetail != null)
        {
            // 更新已有答案
            existingDetail.UserAnswer = dto.UserAnswer;
            existingDetail.TimeSpent = dto.TimeSpent;
            existingDetail.IsCorrect = CheckAnswer(question, dto.UserAnswer);
            await _attemptDetailRepository.UpdateAsync(existingDetail, cancellationToken);
        }
        else
        {
            // 创建新答题详情
            var detail = new AttemptDetail
            {
                AttemptId = dto.AttemptId,
                QuestionId = dto.QuestionId,
                UserAnswer = dto.UserAnswer,
                TimeSpent = dto.TimeSpent,
                IsCorrect = CheckAnswer(question, dto.UserAnswer)
            };
            await _attemptDetailRepository.AddAsync(detail, cancellationToken);
        }

        await _attemptDetailRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AttemptDto> CompleteAttemptAsync(int userId, CompleteAttemptDto dto, CancellationToken cancellationToken = default)
    {
        // 获取答题记录
        var attempt = await _attemptRepository.GetByIdAsync(dto.AttemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            throw new InvalidOperationException("答题记录不存在或无权访问");
        }

        if (attempt.CompletedAt != null)
        {
            throw new InvalidOperationException("答题已完成");
        }

        // 获取所有答题详情
        var details = await _attemptDetailRepository.GetByAttemptIdAsync(dto.AttemptId, cancellationToken);

        // 计算得分
        int correctCount = 0;
        foreach (var detail in details)
        {
            if (detail.IsCorrect == true)
            {
                correctCount++;
            }
        }

        // 更新答题记录
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.Score = details.Count > 0 ? (decimal)correctCount / details.Count * 100 : 0;

        await _attemptRepository.UpdateAsync(attempt, cancellationToken);
        await _attemptRepository.SaveChangesAsync(cancellationToken);

        // 获取题库名称
        var questionBank = await _questionBankRepository.GetByIdAsync(attempt.QuestionBankId, cancellationToken);

        return new AttemptDto
        {
            Id = attempt.Id,
            QuestionBankId = attempt.QuestionBankId,
            QuestionBankName = questionBank?.Name ?? string.Empty,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            Score = attempt.Score,
            TotalQuestions = attempt.TotalQuestions,
            CorrectCount = correctCount,
            DurationSeconds = (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds
        };
    }

    public async Task<AttemptDto?> GetAttemptByIdAsync(int attemptId, int userId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            return null;
        }

        var questionBank = await _questionBankRepository.GetByIdAsync(attempt.QuestionBankId, cancellationToken);
        var details = await _attemptDetailRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        int correctCount = details.Count(d => d.IsCorrect == true);

        return new AttemptDto
        {
            Id = attempt.Id,
            QuestionBankId = attempt.QuestionBankId,
            QuestionBankName = questionBank?.Name ?? string.Empty,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            Score = attempt.Score,
            TotalQuestions = attempt.TotalQuestions,
            CorrectCount = correctCount,
            DurationSeconds = attempt.CompletedAt.HasValue ?
                (int)(attempt.CompletedAt.Value - attempt.StartedAt).TotalSeconds :
                (int?)(DateTime.UtcNow - attempt.StartedAt).TotalSeconds
        };
    }

    public async Task<List<AttemptDetailDto>> GetAttemptDetailsAsync(int attemptId, int userId, CancellationToken cancellationToken = default)
    {
        var attempt = await _attemptRepository.GetByIdAsync(attemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            throw new InvalidOperationException("答题记录不存在或无权访问");
        }

        var details = await _attemptDetailRepository.GetByAttemptIdWithQuestionsAsync(attemptId, cancellationToken);
        var result = new List<AttemptDetailDto>();

        foreach (var detail in details)
        {
            var question = detail.Question;
            string correctAnswer = string.Empty;
            List<string>? options = null;

            // 从新字段 Data 中提取答案和选项
            if (question?.Data is ChoiceQuestionData choiceData)
            {
                options = choiceData.Options;
                correctAnswer = string.Join(",", choiceData.CorrectAnswers);
            }
            else if (question?.Data is BooleanQuestionData booleanData)
            {
                correctAnswer = booleanData.CorrectAnswer.ToString().ToLower();
            }
            else if (question?.Data is FillBlankQuestionData fillData)
            {
                correctAnswer = string.Join(",", fillData.AcceptableAnswers);
            }
            else if (question?.Data is ShortAnswerQuestionData shortData)
            {
                correctAnswer = shortData.ReferenceAnswer;
            }
#pragma warning disable CS0618 // 旧字段兼容性代码：如果没有新数据，使用旧字段
            else if (question != null)
            {
                options = ParseLegacyOptions(question.Options);
                correctAnswer = question.CorrectAnswer;
            }
#pragma warning restore CS0618

            result.Add(new AttemptDetailDto
            {
                Id = detail.Id,
                AttemptId = detail.AttemptId,
                QuestionId = detail.QuestionId,
                QuestionText = detail.Question?.QuestionText ?? string.Empty,
                QuestionType = detail.Question?.QuestionType ?? string.Empty,
                Options = options != null ? string.Join(",", options) : null,
                UserAnswer = detail.UserAnswer,
                CorrectAnswer = correctAnswer,
                IsCorrect = detail.IsCorrect,
                TimeSpent = detail.TimeSpent,
                Explanation = detail.Question?.Explanation
            });
        }

        return result;
    }

    private static List<string>? ParseLegacyOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options))
        {
            return null;
        }

        if (options.Trim().StartsWith("["))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(options);
            }
            catch
            {
                return null;
            }
        }

        return options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    public async Task<AttemptStatisticsDto> GetStatisticsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var attempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);
        var details = await _attemptDetailRepository.GetByAttemptIdWithQuestionsAsync(
            attempts.SelectMany(a => a.AttemptDetails).Select(d => d.Id).FirstOrDefault(),
            cancellationToken);

        int totalAttempts = attempts.Count;
        int completedAttempts = attempts.Count(a => a.CompletedAt != null);
        decimal? averageScore = completedAttempts > 0 ?
            (decimal)attempts.Where(a => a.CompletedAt != null).Average(a => a.Score ?? 0) : null;

        // 获取所有答题详情
        var allDetails = new List<Domain.Entities.AttemptDetail>();
        foreach (var attempt in attempts)
        {
            var attemptDetails = await _attemptDetailRepository.GetByAttemptIdAsync(attempt.Id, cancellationToken);
            allDetails.AddRange(attemptDetails);
        }

        int totalQuestionsAnswered = allDetails.Count;
        int totalCorrectAnswers = allDetails.Count(d => d.IsCorrect == true);
        decimal? overallAccuracy = totalQuestionsAnswered > 0 ?
            (decimal)totalCorrectAnswers / totalQuestionsAnswered * 100 : null;

        return new AttemptStatisticsDto
        {
            TotalAttempts = totalAttempts,
            CompletedAttempts = completedAttempts,
            AverageScore = averageScore,
            TotalQuestionsAnswered = totalQuestionsAnswered,
            TotalCorrectAnswers = totalCorrectAnswers,
            OverallAccuracy = overallAccuracy
        };
    }

    /// <summary>
    /// 检查答案是否正确
    /// </summary>
    private bool CheckAnswer(Domain.Entities.Question question, string userAnswer)
    {
        if (string.IsNullOrWhiteSpace(userAnswer))
        {
            return false;
        }

        // 标准化答案(去除空格、转小写)
        var normalizedUserAnswer = userAnswer.Trim().ToLower();

        // 从新字段 Data 中获取正确答案
        List<string>? correctAnswersList = null;
        bool? boolAnswer = null;
        string? stringAnswer = null;

        if (question.Data is ChoiceQuestionData choiceData)
        {
            correctAnswersList = choiceData.CorrectAnswers;
        }
        else if (question.Data is BooleanQuestionData booleanData)
        {
            boolAnswer = booleanData.CorrectAnswer;
        }
        else if (question.Data is FillBlankQuestionData fillData)
        {
            correctAnswersList = fillData.AcceptableAnswers;
        }
        else if (question.Data is ShortAnswerQuestionData shortData)
        {
            stringAnswer = shortData.ReferenceAnswer;
        }
#pragma warning disable CS0618 // 旧字段兼容性代码：如果没有新数据，使用旧字段
        else
        {
            // 旧格式兼容
            var legacyAnswer = question.CorrectAnswer.Trim().ToLower();
            if (question.QuestionType == "boolean" && bool.TryParse(legacyAnswer, out var legacyBool))
            {
                boolAnswer = legacyBool;
            }
            else if (question.QuestionType == "multiple")
            {
                correctAnswersList = legacyAnswer.Split(',').Select(a => a.Trim()).ToList();
            }
            else
            {
                stringAnswer = legacyAnswer;
            }
        }
#pragma warning restore CS0618

        // 判断题
        if (question.QuestionType == "boolean" || question.QuestionTypeEnum == Domain.Enums.QuestionType.TrueFalse)
        {
            if (boolAnswer.HasValue)
            {
                return normalizedUserAnswer == boolAnswer.Value.ToString().ToLower();
            }
        }

        // 单选题
        if (question.QuestionType == "single" || question.QuestionTypeEnum == Domain.Enums.QuestionType.SingleChoice)
        {
            if (correctAnswersList != null && correctAnswersList.Count > 0)
            {
                return normalizedUserAnswer == correctAnswersList[0].Trim().ToLower();
            }
            if (stringAnswer != null)
            {
                return normalizedUserAnswer == stringAnswer.Trim().ToLower();
            }
        }

        // 多选题(需要排序后比较)
        if (question.QuestionType == "multiple" || question.QuestionTypeEnum == Domain.Enums.QuestionType.MultipleChoice)
        {
            if (correctAnswersList != null)
            {
                var userAnswers = normalizedUserAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var correctAnswersArray = correctAnswersList.Select(a => a.Trim().ToLower()).OrderBy(a => a);
                var userAnswersSorted = userAnswers.Select(a => a.Trim().ToLower()).OrderBy(a => a);
                return userAnswersSorted.SequenceEqual(correctAnswersArray);
            }
        }

        // 填空题和简答题(包含匹配即可)
        if (question.QuestionType == "fill" || question.QuestionType == "essay" ||
            question.QuestionTypeEnum == Domain.Enums.QuestionType.FillBlank ||
            question.QuestionTypeEnum == Domain.Enums.QuestionType.ShortAnswer)
        {
            if (stringAnswer != null)
            {
                var normalizedStringAnswer = stringAnswer.Trim().ToLower();
                return normalizedUserAnswer.Contains(normalizedStringAnswer) ||
                       normalizedStringAnswer.Contains(normalizedUserAnswer);
            }
            if (correctAnswersList != null)
            {
                return correctAnswersList.Any(a =>
                    normalizedUserAnswer.Contains(a.Trim().ToLower()) ||
                    a.Trim().ToLower().Contains(normalizedUserAnswer));
            }
        }

        return false;
    }
}
