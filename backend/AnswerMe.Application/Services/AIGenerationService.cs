using System.Text.Json;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.AI;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceProvider _serviceProvider;

    // 内存中的异步任务存储（生产环境应使用Redis或数据库）
    private static readonly Dictionary<string, AIGenerateProgressDto> _asyncTasks = new();
    private static readonly object _taskLock = new();
    private const int GenerationBatchSize = 5;

    public AIGenerationService(
        IQuestionRepository questionRepository,
        IQuestionBankRepository questionBankRepository,
        IDataSourceRepository dataSourceRepository,
        IDataSourceService dataSourceService,
        AIProviderFactory aiProviderFactory,
        ILogger<AIGenerationService> logger,
        IServiceProvider serviceProvider)
    {
        _questionRepository = questionRepository;
        _questionBankRepository = questionBankRepository;
        _dataSourceRepository = dataSourceRepository;
        _dataSourceService = dataSourceService;
        _aiProviderFactory = aiProviderFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<AIGenerateResponseDto> GenerateQuestionsAsync(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.Count > 20)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "同步生成最多支持20道题目，请使用异步生成接口",
                ErrorCode = "COUNT_EXCEEDED"
            };
        }

        return await GenerateQuestionsInternalAsync(userId, dto, cancellationToken, null);
    }

    public Task<string> StartAsyncGeneration(
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
            UserId = userId,  // 保存用户ID用于权限验证
            Status = "pending",
            GeneratedCount = 0,
            TotalCount = dto.Count,
            CreatedAt = DateTime.UtcNow
        };

        lock (_taskLock)
        {
            _asyncTasks[taskId] = progress;
        }

        // 在后台线程中执行生成任务（fire-and-forget，不等待完成）
        _ = Task.Run(async () => await ExecuteAsyncGeneration(taskId, userId, dto), cancellationToken)
            .ConfigureAwait(false);

        return Task.FromResult(taskId);
    }

    public async Task<AIGenerateProgressDto?> GetProgressAsync(
        int userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        // 使用 await Task.Run 将同步锁操作包装为异步，避免阻塞线程
        return await Task.Run(() =>
        {
            lock (_taskLock)
            {
                if (_asyncTasks.TryGetValue(taskId, out var progress))
                {
                    // 验证用户权限：只能查询自己的任务
                    if (progress.UserId != userId)
                    {
                        return null;  // 返回 null 表示任务不存在（不泄露其他用户任务信息）
                    }

                    // 返回副本避免外部修改（避免序列化抽象类型导致异常）
                    return CloneProgress(progress);
                }
            }
            return null;
        }, cancellationToken);
    }

    /// <summary>
    /// 执行异步生成任务
    /// </summary>
    private async Task ExecuteAsyncGeneration(string taskId, int userId, AIGenerateRequestDto dto)
    {
        try
        {
            // 更新状态为处理中
            UpdateTaskStatus(taskId, "processing");

            // ✅ 修复P0: 创建独立作用域，避免使用已释放的 DbContext
            using var scope = _serviceProvider.CreateScope();

            // 从新作用域获取服务实例（通过接口）
            var aiGenerationService = scope.ServiceProvider.GetRequiredService<IAIGenerationService>();
            var concreteService = aiGenerationService as AIGenerationService
                ?? throw new InvalidOperationException("无法解析 AIGenerationService");

            var response = await concreteService.GenerateQuestionsInternalAsync(
                userId,
                dto,
                CancellationToken.None,
                generatedCount =>
                {
                    UpdateTaskProgress(taskId, generatedCount);
                    return Task.CompletedTask;
                });

            if (response.Success)
            {
                UpdateTaskStatus(taskId, "completed", response.Questions);
            }
            else if (response.PartialSuccessCount.HasValue && response.PartialSuccessCount.Value > 0)
            {
                var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "生成部分成功，但存在失败题目"
                    : response.ErrorMessage;
                UpdateTaskStatus(taskId, "partial_success", response.Questions, errorMessage);
            }
            else
            {
                var errorMessage = string.IsNullOrWhiteSpace(response.ErrorMessage)
                    ? "AI生成失败，请查看服务器日志"
                    : response.ErrorMessage;
                UpdateTaskStatus(taskId, "failed", errorMessage: errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异步生成任务失败: TaskId={TaskId}", taskId);
            UpdateTaskStatus(taskId, "failed", errorMessage: ex.Message);
        }
    }

    /// <summary>
    /// 更新任务状态
    /// </summary>
    private void UpdateTaskStatus(
        string taskId,
        string status,
        List<GeneratedQuestionDto>? questions = null,
        string? errorMessage = null)
    {
        lock (_taskLock)
        {
            if (_asyncTasks.TryGetValue(taskId, out var progress))
            {
                progress.Status = status;
                progress.GeneratedCount = questions?.Count ?? 0;
                progress.Questions = questions;
                progress.ErrorMessage = errorMessage;

                if (status is "completed" or "failed" or "partial_success")
                {
                    progress.CompletedAt = DateTime.UtcNow;
                }
            }
        }
    }

    private void UpdateTaskProgress(string taskId, int generatedCount)
    {
        lock (_taskLock)
        {
            if (_asyncTasks.TryGetValue(taskId, out var progress))
            {
                progress.Status = "processing";
                progress.GeneratedCount = generatedCount;
            }
        }
    }

    private async Task<AIGenerateResponseDto> GenerateQuestionsInternalAsync(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken,
        Func<int, Task>? onProgress)
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

        var dataSource = await GetDataSourceForUserAsync(userId, dto.ProviderName, cancellationToken);
        if (dataSource == null)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "未找到有效的AI配置，请先配置API密钥",
                ErrorCode = "NO_DATA_SOURCE"
            };
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

        var provider = _aiProviderFactory.GetProvider(dataSource.Type);
        if (provider == null)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = $"不支持的AI Provider: {dataSource.Type}",
                ErrorCode = "UNSUPPORTED_PROVIDER"
            };
        }

        var config = await _dataSourceService.GetDecryptedConfigAsync(dataSource.Id, userId, cancellationToken);
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            _logger.LogError("无法获取数据源 {DataSourceId} 的解密配置", dataSource.Id);
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "数据源配置解密失败，请检查配置",
                ErrorCode = "CONFIG_DECRYPTION_FAILED"
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
            var batchSize = Math.Min(GenerationBatchSize, remaining);
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

            foreach (var question in aiResponse.Questions)
            {
                try
                {
                    var resolvedType = ResolveQuestionType(question.QuestionTypeEnum, question.Data, question.QuestionType);
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

                    var data = NormalizeQuestionData(
                        question.Data,
                        resolvedType,
                        question.Options,
                        question.CorrectAnswer,
                        question.Explanation,
                        question.Difficulty);

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

                    var resultDto = new GeneratedQuestionDto
                    {
                        Id = questionEntity.Id,
                        QuestionTypeEnum = questionEntity.QuestionTypeEnum,
                        QuestionText = question.QuestionText,
                        Data = questionEntity.Data,
                        Options = question.Options,
                        CorrectAnswer = question.CorrectAnswer,
                        Explanation = question.Explanation,
                        Difficulty = question.Difficulty,
                        QuestionBankId = questionBankId,
                        CreatedAt = questionEntity.CreatedAt
                    };
                    resultDto.PopulateLegacyFieldsFromData();
                    savedQuestions.Add(resultDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "保存题目失败: {QuestionText}", question.QuestionText);
                }
            }

            try
            {
                await _questionRepository.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "提交保存题目事务失败");
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
                await onProgress(savedQuestions.Count);
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
                // 调用AI生成，传递 apiKey、model 和 endpoint
                var response = await provider.GenerateQuestionsAsync(apiKey, request, model, endpoint, cancellationToken);

                if (response.Success)
                {
                    return response;
                }

                // 如果不是临时错误，直接返回
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

            // 指数退避
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

        // 返回第一个可用的数据源
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
        return questionBank.Id;
    }

    /// <summary>
    /// 映射Question到GeneratedQuestionDto
    /// </summary>
    private static GeneratedQuestionDto MapToGeneratedQuestionDto(Question question)
    {
        var options = new List<string>();
        if (!string.IsNullOrEmpty(question.Options))
        {
            try
            {
                options = JsonSerializer.Deserialize<List<string>>(question.Options) ?? new List<string>();
            }
            catch
            {
                options = new List<string>();
            }
        }

        var dto = new GeneratedQuestionDto
        {
            Id = question.Id,
            QuestionTypeEnum = question.QuestionTypeEnum,
            QuestionText = question.QuestionText,
            Data = question.Data,
            Options = options,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            QuestionBankId = question.QuestionBankId,
            CreatedAt = question.CreatedAt
        };
        dto.PopulateLegacyFieldsFromData();
        return dto;
    }

    private static QuestionType? ResolveQuestionType(QuestionType? requestedType, QuestionData? data, string? legacyType)
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
        List<string>? legacyOptions,
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
        List<string>? legacyOptions,
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
                var options = legacyOptions ?? new List<string>();
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

    private static AIGenerateProgressDto CloneProgress(AIGenerateProgressDto progress)
    {
        return new AIGenerateProgressDto
        {
            TaskId = progress.TaskId,
            UserId = progress.UserId,
            Status = progress.Status,
            GeneratedCount = progress.GeneratedCount,
            TotalCount = progress.TotalCount,
            ErrorMessage = progress.ErrorMessage,
            Questions = progress.Questions?.Select(CloneGeneratedQuestion).ToList(),
            CreatedAt = progress.CreatedAt,
            CompletedAt = progress.CompletedAt
        };
    }

    private static GeneratedQuestionDto CloneGeneratedQuestion(GeneratedQuestionDto question)
    {
        return new GeneratedQuestionDto
        {
            Id = question.Id,
            QuestionTypeEnum = question.QuestionTypeEnum,
            QuestionText = question.QuestionText,
            Data = question.Data,
            Options = question.Options != null ? new List<string>(question.Options) : new List<string>(),
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            QuestionBankId = question.QuestionBankId,
            CreatedAt = question.CreatedAt
        };
    }
}
