using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Common;

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
        if (question.QuestionBankId != attempt.QuestionBankId)
        {
            throw new InvalidOperationException("题目不属于本次答题");
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
        // 使用本次答题总题数作为分母，避免通过只提交部分题目抬高得分。
        attempt.Score = attempt.TotalQuestions > 0 ? (decimal)correctCount / attempt.TotalQuestions * 100 : 0;

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
        var parsed = LegacyFieldParser.ParseDelimitedList(options);
        return parsed.Count == 0 ? null : parsed;
    }

    public async Task<AttemptStatisticsDto> GetStatisticsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var attempts = await _attemptRepository.GetByUserIdAsync(userId, cancellationToken);

        int totalAttempts = attempts.Count;
        int completedAttempts = attempts.Count(a => a.CompletedAt != null);
        decimal? averageScore = completedAttempts > 0 ?
            (decimal)attempts.Where(a => a.CompletedAt != null).Average(a => a.Score ?? 0) : null;

        // 获取所有答题详情（优化：使用批量查询避免 N+1 问题）
        var attemptIds = attempts.Select(a => a.Id).ToList();
        var allDetails = await _attemptDetailRepository.GetByAttemptIdsAsync(attemptIds, cancellationToken);

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

    public async Task<WrongQuestionListDto> GetWrongQuestionsAsync(int userId, WrongQuestionQueryDto query, CancellationToken cancellationToken = default)
    {
        var details = await _attemptDetailRepository.GetWrongQuestionsAsync(
            userId,
            query.QuestionBankId,
            query.QuestionType,
            query.StartDate,
            query.EndDate,
            cancellationToken);

        // 过滤掉已掌握的错题
        var unmasteredDetails = details.Where(d => !d.IsMastered).ToList();

        var result = new List<WrongQuestionDto>();

        foreach (var detail in unmasteredDetails)
        {
            var question = detail.Question;
            string correctAnswer = string.Empty;
            string? options = null;

            if (question?.Data is ChoiceQuestionData choiceData)
            {
                options = string.Join(",", choiceData.Options);
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
#pragma warning disable CS0618
            else if (question != null)
            {
                options = question.Options;
                correctAnswer = question.CorrectAnswer ?? string.Empty;
            }
#pragma warning restore CS0618

            result.Add(new WrongQuestionDto
            {
                Id = detail.Id,
                QuestionId = detail.QuestionId,
                QuestionText = question?.QuestionText ?? string.Empty,
                QuestionType = question?.QuestionType ?? string.Empty,
                Options = options,
                UserAnswer = detail.UserAnswer ?? string.Empty,
                CorrectAnswer = correctAnswer,
                Explanation = question?.Explanation,
                AttemptId = detail.AttemptId,
                AnsweredAt = detail.Attempt?.StartedAt ?? DateTime.UtcNow,
                QuestionBankId = detail.Attempt?.QuestionBankId ?? 0,
                QuestionBankName = detail.Attempt?.QuestionBank?.Name ?? string.Empty,
                IsMastered = detail.IsMastered
            });
        }

        var bankGroupCount = result.GroupBy(q => q.QuestionBankId).Count();

        return new WrongQuestionListDto
        {
            Questions = result,
            TotalCount = result.Count,
            BankGroupCount = bankGroupCount
        };
    }

    public async Task<LearningStatsDto> GetLearningStatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var (totalAttempts, totalQuestions, correctCount, wrongCount, totalTimeSpent) =
            await _attemptDetailRepository.GetLearningStatsAsync(userId, cancellationToken);

        var weeklyTrend = await _attemptDetailRepository.GetWeeklyTrendAsync(userId, 12, cancellationToken);
        var bankStats = await _attemptDetailRepository.GetBankStatsAsync(userId, cancellationToken);

        var accuracyRate = totalQuestions > 0 ? Math.Round((decimal)correctCount / totalQuestions * 100, 2) : 0;
        var averageTimePerQuestion = totalQuestions > 0 ? Math.Round((decimal)totalTimeSpent / totalQuestions, 1) : 0;

        var weeklyTrendDto = weeklyTrend.Select(w => new WeeklyStatDto
        {
            WeekStart = w.weekStart,
            AttemptCount = w.attemptCount,
            QuestionCount = w.questionCount,
            CorrectCount = w.correctCount,
            AccuracyRate = w.questionCount > 0 ? Math.Round((decimal)w.correctCount / w.questionCount * 100, 2) : 0
        }).ToList();

        var bankStatsDto = bankStats.Select(b => new BankStatDto
        {
            QuestionBankId = b.questionBankId,
            QuestionBankName = b.questionBankName,
            AttemptCount = b.attemptCount,
            TotalQuestions = b.totalQuestions,
            CorrectCount = b.correctCount,
            AccuracyRate = b.totalQuestions > 0 ? (int)Math.Round((decimal)b.correctCount / b.totalQuestions * 100) : 0
        }).ToList();

        return new LearningStatsDto
        {
            TotalAttempts = totalAttempts,
            TotalQuestions = totalQuestions,
            CorrectCount = correctCount,
            WrongCount = wrongCount,
            AccuracyRate = accuracyRate,
            AverageTimePerQuestion = averageTimePerQuestion,
            TotalTimeSpent = totalTimeSpent,
            WeeklyTrend = weeklyTrendDto,
            BankStats = bankStatsDto
        };
    }

    public async Task MarkQuestionAsMasteredAsync(int userId, int attemptDetailId, CancellationToken cancellationToken = default)
    {
        var detail = await _attemptDetailRepository.GetByIdAsync(attemptDetailId, cancellationToken);
        if (detail == null)
        {
            throw new InvalidOperationException("答题详情不存在");
        }

        // 验证权限
        var attempt = await _attemptRepository.GetByIdAsync(detail.AttemptId, cancellationToken);
        if (attempt == null || attempt.UserId != userId)
        {
            throw new InvalidOperationException("无权访问此答题记录");
        }

        detail.IsMastered = true;
        detail.MasteredAt = DateTime.UtcNow;
        await _attemptDetailRepository.UpdateAsync(detail, cancellationToken);
        await _attemptDetailRepository.SaveChangesAsync(cancellationToken);
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

        var normalizedUserAnswer = userAnswer.Trim().ToLower();
        var answerInfo = ExtractAnswerInfo(question);

        return question.QuestionTypeEnum switch
        {
            Domain.Enums.QuestionType.TrueFalse => CheckBooleanAnswer(normalizedUserAnswer, answerInfo.BoolAnswer),
            Domain.Enums.QuestionType.SingleChoice => CheckSingleChoiceAnswer(normalizedUserAnswer, answerInfo),
            Domain.Enums.QuestionType.MultipleChoice => CheckMultipleChoiceAnswer(normalizedUserAnswer, answerInfo.CorrectAnswersList),
            Domain.Enums.QuestionType.FillBlank => CheckContainsAnswer(normalizedUserAnswer, answerInfo),
            Domain.Enums.QuestionType.ShortAnswer => CheckContainsAnswer(normalizedUserAnswer, answerInfo),
            _ => CheckLegacyAnswer(normalizedUserAnswer, question.QuestionType, answerInfo)
        };
    }

    private static bool CheckBooleanAnswer(string userAnswer, bool? correctAnswer)
    {
        return correctAnswer.HasValue && userAnswer == correctAnswer.Value.ToString().ToLower();
    }

    private static bool CheckSingleChoiceAnswer(string userAnswer, AnswerInfo info)
    {
        if (info.CorrectAnswersList != null && info.CorrectAnswersList.Count > 0)
        {
            return userAnswer == info.CorrectAnswersList[0].Trim().ToLower();
        }
        if (info.StringAnswer != null)
        {
            return userAnswer == info.StringAnswer.Trim().ToLower();
        }
        return false;
    }

    private static bool CheckMultipleChoiceAnswer(string userAnswer, List<string>? correctAnswers)
    {
        if (correctAnswers == null) return false;

        // 尝试解析用户答案（兼容 JSON 数组和逗号分隔格式）
        List<string> userAnswers;
        if (userAnswer.TrimStart().StartsWith('['))
        {
            // JSON 数组格式: ["A", "B"] 或 ["A","B"]
            try
            {
                userAnswers = System.Text.Json.JsonSerializer.Deserialize<List<string>>(userAnswer) ?? new List<string>();
            }
            catch
            {
                // JSON 解析失败，回退到逗号分隔
                userAnswers = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }
        else
        {
            // 逗号分隔格式: A,B 或 A, B
            userAnswers = userAnswer.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        var sortedCorrect = correctAnswers.Select(a => a.Trim().ToLower()).OrderBy(a => a);
        var sortedUser = userAnswers.Select(a => a.Trim().ToLower()).OrderBy(a => a);
        return sortedUser.SequenceEqual(sortedCorrect);
    }

    private static bool CheckContainsAnswer(string userAnswer, AnswerInfo info)
    {
        if (info.StringAnswer != null)
        {
            var normalizedAnswer = info.StringAnswer.Trim().ToLower();
            return userAnswer.Contains(normalizedAnswer) || normalizedAnswer.Contains(userAnswer);
        }
        if (info.CorrectAnswersList != null)
        {
            return info.CorrectAnswersList.Any(a =>
                userAnswer.Contains(a.Trim().ToLower()) || a.Trim().ToLower().Contains(userAnswer));
        }
        return false;
    }

    private static bool CheckLegacyAnswer(string userAnswer, string questionType, AnswerInfo info)
    {
        return questionType switch
        {
            "boolean" => CheckBooleanAnswer(userAnswer, info.BoolAnswer),
            "single" => CheckSingleChoiceAnswer(userAnswer, info),
            "multiple" => CheckMultipleChoiceAnswer(userAnswer, info.CorrectAnswersList),
            "fill" or "essay" => CheckContainsAnswer(userAnswer, info),
            _ => false
        };
    }

    private static AnswerInfo ExtractAnswerInfo(Domain.Entities.Question question)
    {
        var info = new AnswerInfo();

        switch (question.Data)
        {
            case ChoiceQuestionData choiceData:
                info.CorrectAnswersList = choiceData.CorrectAnswers;
                break;
            case BooleanQuestionData booleanData:
                info.BoolAnswer = booleanData.CorrectAnswer;
                break;
            case FillBlankQuestionData fillData:
                info.CorrectAnswersList = fillData.AcceptableAnswers;
                break;
            case ShortAnswerQuestionData shortData:
                info.StringAnswer = shortData.ReferenceAnswer;
                break;
            default:
#pragma warning disable CS0618 // 旧字段兼容性代码
                if (!string.IsNullOrWhiteSpace(question.CorrectAnswer))
                {
                    var legacyAnswer = question.CorrectAnswer.Trim().ToLower();
                    switch (question.QuestionType)
                    {
                        case "boolean":
                            info.BoolAnswer = LegacyFieldParser.ParseBooleanAnswer(legacyAnswer);
                            if (info.BoolAnswer.HasValue)
                            {
                                break;
                            }
                            info.StringAnswer = legacyAnswer;
                            break;
                        case "multiple":
                            info.CorrectAnswersList = legacyAnswer.Split(',').Select(a => a.Trim()).ToList();
                            break;
                        default:
                            info.StringAnswer = legacyAnswer;
                            break;
                    }
                }
#pragma warning restore CS0618
                break;
        }

        return info;
    }

    private class AnswerInfo
    {
        public List<string>? CorrectAnswersList { get; set; }
        public bool? BoolAnswer { get; set; }
        public string? StringAnswer { get; set; }
    }
}
