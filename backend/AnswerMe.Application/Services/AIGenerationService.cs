using System.Text.Json;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.AI;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace AnswerMe.Application.Services;

/// <summary>
/// AI生成题目服务实现
/// </summary>
public class AIGenerationService : IAIGenerationService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataSourceService _dataSourceService;
    private readonly AIProviderFactory _aiProviderFactory;
    private readonly ILogger<AIGenerationService> _logger;
    private readonly IAIGenerationTaskQueue _taskQueue;
    private readonly IAIGenerationProgressStore _progressStore;
    private readonly AIGenerationOptions _options;

    public AIGenerationService(
        IQuestionRepository questionRepository,
        IQuestionBankRepository questionBankRepository,
        IDataSourceRepository dataSourceRepository,
        IDataSourceService dataSourceService,
        AIProviderFactory aiProviderFactory,
        ILogger<AIGenerationService> logger,
        IAIGenerationTaskQueue taskQueue,
        IAIGenerationProgressStore progressStore,
        IOptions<AIGenerationOptions> options)
    {
        _questionRepository = questionRepository;
        _questionBankRepository = questionBankRepository;
        _dataSourceRepository = dataSourceRepository;
        _dataSourceService = dataSourceService;
        _aiProviderFactory = aiProviderFactory;
        _logger = logger;
        _taskQueue = taskQueue;
        _progressStore = progressStore;
        _options = options.Value;
    }

    public async Task<AIGenerateResponseDto> GenerateQuestionsAsync(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Count > _options.MaxSyncCount)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = $"同步生成最多支持{_options.MaxSyncCount}道题目，请使用异步生成接口",
                ErrorCode = "COUNT_EXCEEDED"
            };
        }

        return await GenerateQuestionsInternalAsync(userId, dto, null, cancellationToken);
    }

    public async Task<string> StartAsyncGeneration(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // 生成任务ID
        var taskId = Guid.NewGuid().ToString();

        // 初始化任务状态
        var progress = new AIGenerateProgressDto
        {
            TaskId = taskId,
            UserId = userId,
            Status = "pending",
            GeneratedCount = 0,
            TotalCount = dto.Count,
            CreatedAt = DateTime.UtcNow
        };

        // 保存初始进度到 Redis
        await _progressStore.SetAsync(taskId, progress, TimeSpan.FromHours(_options.TaskTtlHours), cancellationToken);

        // 将任务加入队列
        await _taskQueue.EnqueueAsync(taskId, userId, dto, cancellationToken);

        _logger.LogInformation("任务已加入队列: {TaskId}, 用户: {UserId}, 题目数量: {Count}", taskId, userId, dto.Count);

        return taskId;
    }

    public async Task<AIGenerateProgressDto?> GetProgressAsync(
        int userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        var progress = await _progressStore.GetAsync(taskId, cancellationToken);

        if (progress == null)
        {
            return null;
        }

        // 验证用户权限：只能查询自己的任务
        if (progress.UserId != userId)
        {
            return null;
        }

        return progress;
    }

    /// <summary>
    /// 执行后台任务（由 Worker 调用）
    /// </summary>
    public async Task ExecuteTaskAsync(
        string taskId,
        int userId,
        AIGenerateRequestDto dto,
        Func<string, int, int, string, Task> progressCallback,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 更新状态为处理中
            await _progressStore.UpdateAsync(taskId, progress => progress.Status = "processing", cancellationToken);

            var response = await GenerateQuestionsInternalAsync(
                userId,
                dto,
                (generatedCount, totalCount) =>
                {
                    return progressCallback(taskId, generatedCount, totalCount, "processing");
                },
                cancellationToken);

            if (response.Success)
            {
                await _progressStore.UpdateAsync(taskId, progress =>
                {
                    progress.Status = "completed";
                    progress.GeneratedCount = response.Questions.Count;
                    progress.Questions = response.Questions;
                    progress.CompletedAt = DateTime.UtcNow;
                }, cancellationToken);
            }
            else if (response.PartialSuccessCount.HasValue && response.PartialSuccessCount.Value > 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "生成部分成功，但存在失败题目"
                    : response.ErrorMessage;
                await _progressStore.UpdateAsync(taskId, progress =>
                {
                    progress.Status = "partial_success";
                    progress.GeneratedCount = response.Questions.Count;
                    progress.Questions = response.Questions;
                    progress.ErrorMessage = errorMessage;
                    progress.CompletedAt = DateTime.UtcNow;
                }, cancellationToken);
            }
            else
            {
                var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "AI生成失败，请查看服务器日志"
                    : response.ErrorMessage;
                await _progressStore.UpdateAsync(taskId, progress =>
                {
                    progress.Status = "failed";
                    progress.ErrorMessage = errorMessage;
                    progress.CompletedAt = DateTime.UtcNow;
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步生成任务失败: TaskId={TaskId}", taskId);
            await _progressStore.UpdateAsync(taskId, progress =>
            {
                progress.Status = "failed";
                progress.ErrorMessage = ex.Message;
                progress.CompletedAt = DateTime.UtcNow;
            }, cancellationToken);
        }
    }

    private async Task<AIGenerateResponseDto> GenerateQuestionsInternalAsync(
        int userId,
        AIGenerateRequestDto dto,
        Func<int, int, Task>? onProgress,
        CancellationToken cancellationToken)
    {
        // 验证请求参数
        var validationResult = ValidateRequest(dto);
        if (validationResult != null)
        {
            return validationResult;
        }

        if (dto.QuestionBankId.HasValue)
        {
            var questionBank = await _questionBankRepository.GetByIdAsync(dto.QuestionBankId.Value, cancellationToken);
            if (questionBank == null || questionBank.UserId != userId)
            {
                return new AIGenerateResponseDto
                {
                    Success = false,
                    ErrorMessage = "题库不存在或无权访问",
                    ErrorCode = "QUESTIONBANK_NOT_FOUND"
                };
            }
        }

        // 验证数据源配置
        var (dataSource, config, provider) = await ValidateDataSourceAsync(userId, dto, cancellationToken);
        if (dataSource == null)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "未找到有效的AI配置，请先配置API密钥",
                ErrorCode = "NO_DATA_SOURCE"
            };
        }
        if (config == null)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "数据源配置解密失败，请检查配置",
                ErrorCode = "CONFIG_DECRYPTION_FAILED"
            };
        }
        if (provider == null)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = $"不支持的AI Provider: {dataSource.Type}",
                ErrorCode = "UNSUPPORTED_PROVIDER"
            };
        }

        var savedQuestions = new List<GeneratedQuestionDto>();
        var totalRequested = dto.Count;
        var remaining = dto.Count;
        var questionBankId = dto.QuestionBankId ?? await CreateDefaultQuestionBankAsync(userId, cancellationToken);
        var tokensUsedTotal = 0;
        var hasTokens = false;

        while (remaining > 0)
        {
            var batchSize = Math.Min(_options.BatchSize, remaining);
            var aiRequest = new AIQuestionGenerateRequest
            {
                Subject = dto.Subject,
                Count = batchSize,
                Difficulty = dto.Difficulty,
                QuestionTypes = dto.QuestionTypes,
                Language = dto.Language,
                CustomPrompt = dto.CustomPrompt
            };

            var aiResponse = await CallAIWithRetryAsync(
                provider,
                config.ApiKey,
                aiRequest,
                config.Model,
                config.Endpoint,
                maxRetries: 3,
                cancellationToken);

            if (!aiResponse.Success || aiResponse.Questions.Count == 0)
            {
                return new AIGenerateResponseDto
                {
                    Success = savedQuestions.Count > 0,
                    Questions = savedQuestions,
                    ErrorMessage = aiResponse.ErrorMessage ?? "AI生成失败",
                    ErrorCode = aiResponse.ErrorCode ?? "AI_GENERATION_FAILED",
                    PartialSuccessCount = savedQuestions.Count > 0 ? savedQuestions.Count : null,
                    TokensUsed = hasTokens ? tokensUsedTotal : null
                };
            }

            if (aiResponse.TokensUsed.HasValue)
            {
                tokensUsedTotal += aiResponse.TokensUsed.Value;
                hasTokens = true;
            }

            var savedBeforeBatch = savedQuestions.Count;
            var pendingEntities = new List<Question>();

            foreach (var question in aiResponse.Questions)
            {
                try
                {
#pragma warning disable CS0618 // 旧字段兼容性代码：从 GeneratedQuestion 读取
                    var resolvedType = ResolveQuestionType(question.QuestionTypeEnum, question.Data, question.QuestionType);
#pragma warning restore CS0618
                    if (resolvedType == null && dto.QuestionTypes.Count == 1)
                    {
                        resolvedType = dto.QuestionTypes[0];
                    }
                    if (resolvedType == null || string.IsNullOrWhiteSpace(question.QuestionText))
                    {
                        _logger.LogWarning(
                            "跳过无效题目: Type={Type}, TextLength={Length}",
                            resolvedType?.ToString() ?? "null",
                            question.QuestionText?.Length ?? 0);
                        continue;
                    }

#pragma warning disable CS0618 // 旧字段兼容性代码：从 GeneratedQuestion 读取
                    var data = NormalizeQuestionData(
                        question.Data,
                        resolvedType,
                        question.Options,
                        question.CorrectAnswer,
                        question.Explanation,
                        question.Difficulty);
#pragma warning restore CS0618

                    if (data == null)
                    {
                        _logger.LogWarning(
                            "跳过无效题目数据: Type={Type}, TextLength={Length}",
                            resolvedType?.ToString() ?? "null",
                            question.QuestionText?.Length ?? 0);
                        continue;
                    }

                    var questionEntity = new Question
                    {
                        QuestionTypeEnum = resolvedType,
                        QuestionText = question.QuestionText,
                        Explanation = question.Explanation,
                        Difficulty = question.Difficulty,
                        QuestionBankId = questionBankId,
                        CreatedAt = DateTime.UtcNow
                    };

                    ApplyQuestionData(questionEntity, data);
                    await _questionRepository.AddAsync(questionEntity, cancellationToken);
                    pendingEntities.Add(questionEntity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存题目失败: {QuestionText}", question.QuestionText);
                }
            }

            if (pendingEntities.Count == 0)
            {
                return new AIGenerateResponseDto
                {
                    Success = savedQuestions.Count > 0,
                    Questions = savedQuestions,
                    ErrorMessage = "AI响应格式不正确或题目内容为空，请调整提示词或模型",
                    ErrorCode = "INVALID_AI_RESPONSE",
                    PartialSuccessCount = savedQuestions.Count > 0 ? savedQuestions.Count : null,
                    TokensUsed = hasTokens ? tokensUsedTotal : null
                };
            }

            try
            {
                await _questionRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交保存题目事务失败");
                return new AIGenerateResponseDto
                {
                    Success = savedQuestions.Count > 0,
                    Questions = savedQuestions,
                    ErrorMessage = "保存题目失败，请稍后重试",
                    ErrorCode = "PERSIST_FAILED",
                    PartialSuccessCount = savedQuestions.Count > 0 ? savedQuestions.Count : null,
                    TokensUsed = hasTokens ? tokensUsedTotal : null
                };
            }

            foreach (var entity in pendingEntities)
            {
                var resultDto = new GeneratedQuestionDto
                {
                    Id = entity.Id,
                    QuestionTypeEnum = entity.QuestionTypeEnum,
                    QuestionText = entity.QuestionText,
                    Data = entity.Data,
                    Explanation = entity.Explanation,
                    Difficulty = entity.Difficulty,
                    QuestionBankId = entity.QuestionBankId,
                    CreatedAt = entity.CreatedAt
                };
                resultDto.PopulateLegacyFieldsFromData();
                savedQuestions.Add(resultDto);
            }

            if (savedQuestions.Count == savedBeforeBatch)
            {
                return new AIGenerateResponseDto
                {
                    Success = savedQuestions.Count > 0,
                    Questions = savedQuestions,
                    ErrorMessage = "AI响应格式不正确或题目内容为空，请调整提示词或模型",
                    ErrorCode = "INVALID_AI_RESPONSE",
                    PartialSuccessCount = savedQuestions.Count > 0 ? savedQuestions.Count : null,
                    TokensUsed = hasTokens ? tokensUsedTotal : null
                };
            }

            remaining -= batchSize;

            if (onProgress != null)
            {
                await onProgress(savedQuestions.Count, totalRequested);
            }
        }

        if (savedQuestions.Count == 0)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "AI响应格式不正确或题目内容为空，请调整提示词或模型",
                ErrorCode = "INVALID_AI_RESPONSE"
            };
        }

        var isPartialSuccess = savedQuestions.Count < totalRequested;

        return new AIGenerateResponseDto
        {
            Success = true,
            Questions = savedQuestions,
            TokensUsed = hasTokens ? tokensUsedTotal : null,
            PartialSuccessCount = isPartialSuccess ? savedQuestions.Count : null,
            ErrorMessage = isPartialSuccess ? $"成功保存 {savedQuestions.Count}/{totalRequested} 道题目，部分题目保存失败" : null,
            ErrorCode = isPartialSuccess ? "PARTIAL_SUCCESS" : null
        };
    }

    /// <summary>
    /// 调用AI生成（带重试机制）
    /// </summary>
    private async Task<AIQuestionGenerateResponse> CallAIWithRetryAsync(
        IAIProvider provider,
        string apiKey,
        AIQuestionGenerateRequest request,
        string? model,
        string? endpoint,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        var lastError = default(Exception);

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                var response = await provider.GenerateQuestionsAsync(apiKey, request, model, endpoint, cancellationToken);

                if (response.Success)
                {
                    return response;
                }

                if (response.ErrorCode != "RATE_LIMIT_EXCEEDED" &&
                    response.ErrorCode != "TIMEOUT" &&
                    response.ErrorCode != "SERVICE_UNAVAILABLE")
                {
                    return response;
                }

                lastError = new Exception(response.ErrorMessage ?? "Unknown error");
            }
            catch (Exception ex)
            {
                lastError = ex;
                _logger.LogWarning(ex, "AI生成失败，重试 {Retry}/{MaxRetries}", retry + 1, maxRetries);
            }

            if (retry < maxRetries - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retry));
                await Task.Delay(delay, cancellationToken);
            }
        }

        return new AIQuestionGenerateResponse
        {
            Success = false,
            ErrorMessage = lastError?.Message ?? "AI生成失败，已达到最大重试次数",
            ErrorCode = "MAX_RETRIES_EXCEEDED"
        };
    }

    /// <summary>
    /// 验证请求参数
    /// </summary>
    private static AIGenerateResponseDto? ValidateRequest(AIGenerateRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Subject))
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "生成主题不能为空",
                ErrorCode = "INVALID_SUBJECT"
            };
        }

        if (dto.Count <= 0)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "生成数量必须大于0",
                ErrorCode = "INVALID_COUNT"
            };
        }

        return null;
    }

    /// <summary>
    /// 验证数据源配置
    /// </summary>
    private async Task<(Domain.Entities.DataSource? dataSource, DataSourceConfigDto? config, IAIProvider? provider)>
        ValidateDataSourceAsync(int userId, AIGenerateRequestDto dto, CancellationToken cancellationToken)
    {
        var dataSource = await GetDataSourceForUserAsync(userId, dto.ProviderName, cancellationToken);
        if (dataSource == null)
        {
            return (null, null, null);
        }

        var provider = _aiProviderFactory.GetProvider(dataSource.Type);
        var config = await _dataSourceService.GetDecryptedConfigAsync(dataSource.Id, userId, cancellationToken);

        return (dataSource, config, provider);
    }

    /// <summary>
    /// 获取用户的数据源
    /// </summary>
    private async Task<DataSource?> GetDataSourceForUserAsync(
        int userId,
        string? providerName,
        CancellationToken cancellationToken)
    {
        var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);

        if (!string.IsNullOrEmpty(providerName))
        {
            return dataSources.FirstOrDefault(ds =>
                ds.Type.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        }

        return dataSources.FirstOrDefault();
    }

    /// <summary>
    /// 创建默认题库
    /// </summary>
    private async Task<int> CreateDefaultQuestionBankAsync(int userId, CancellationToken cancellationToken)
    {
        var questionBank = new QuestionBank
        {
            Name = "AI生成题目",
            Description = "通过AI自动生成的题目",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _questionBankRepository.AddAsync(questionBank, cancellationToken);
        await _questionBankRepository.SaveChangesAsync(cancellationToken);
        return questionBank.Id;
    }

    private static QuestionType? ResolveQuestionType(
        QuestionType? requestedType,
        QuestionData? data,
#pragma warning disable CS0618 // 旧字段兼容性代码：legacyType 参数
        string? legacyType)
#pragma warning restore CS0618
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

        if (!string.IsNullOrWhiteSpace(legacyType))
        {
            return QuestionTypeExtensions.ParseFromString(legacyType);
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
#pragma warning disable CS0618 // 旧字段兼容性代码：legacyOptions/legacyCorrectAnswer 参数
        List<string>? legacyOptions,
        string? legacyCorrectAnswer,
#pragma warning restore CS0618
        string? explanation,
        string? difficulty)
    {
        if (data == null)
        {
#pragma warning disable CS0618 // 旧字段兼容性代码：从旧字段构建数据
            data = BuildDataFromLegacy(questionType, legacyOptions, legacyCorrectAnswer, explanation, difficulty);
#pragma warning restore CS0618
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

    private static void ApplyQuestionData(Question question, QuestionData? data)
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
    }

    private static QuestionData? BuildDataFromLegacy(
        QuestionType? questionType,
#pragma warning disable CS0618 // 旧字段兼容性代码：legacyOptions/legacyCorrectAnswer 参数
        List<string>? legacyOptions,
        string? legacyCorrectAnswer,
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                var options = legacyOptions ?? new List<string>();
                var correctAnswers = ParseLegacyAnswers(legacyCorrectAnswer);
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                var booleanAnswer = LegacyFieldParser.ParseBooleanAnswer(legacyCorrectAnswer);
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                var answers = ParseLegacyAnswers(legacyCorrectAnswer);
#pragma warning restore CS0618
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
#pragma warning disable CS0618 // 旧字段兼容性代码
                if (string.IsNullOrWhiteSpace(legacyCorrectAnswer) && string.IsNullOrWhiteSpace(explanation))
#pragma warning restore CS0618
                {
                    return null;
                }
                return new ShortAnswerQuestionData
                {
#pragma warning disable CS0618 // 旧字段兼容性代码
                    ReferenceAnswer = legacyCorrectAnswer ?? string.Empty,
#pragma warning restore CS0618
                    Explanation = explanation,
                    Difficulty = difficulty ?? "medium"
                };
            }
            default:
                return null;
        }
    }

    private static List<string> ParseLegacyAnswers(
#pragma warning disable CS0618 // 旧字段兼容性代码
        string? legacyAnswers)
#pragma warning restore CS0618
    {
        return LegacyFieldParser.ParseCorrectAnswers(legacyAnswers);
    }
}
