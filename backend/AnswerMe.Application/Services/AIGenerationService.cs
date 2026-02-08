using System.Text.Json;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.AI;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Application.Services;

/// <summary>
/// AI生成题目服务实现
/// </summary>
public class AIGenerationService : IAIGenerationService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly AIProviderFactory _aiProviderFactory;
    private readonly ILogger<AIGenerationService> _logger;
    private readonly IServiceProvider _serviceProvider;

    // 内存中的异步任务存储（生产环境应使用Redis或数据库）
    private static readonly Dictionary<string, AIGenerateProgressDto> _asyncTasks = new();
    private static readonly object _taskLock = new();

    public AIGenerationService(
        IQuestionRepository questionRepository,
        IQuestionBankRepository questionBankRepository,
        IDataSourceRepository dataSourceRepository,
        AIProviderFactory aiProviderFactory,
        ILogger<AIGenerationService> logger,
        IServiceProvider serviceProvider)
    {
        _questionRepository = questionRepository;
        _questionBankRepository = questionBankRepository;
        _dataSourceRepository = dataSourceRepository;
        _aiProviderFactory = aiProviderFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task<AIGenerateResponseDto> GenerateQuestionsAsync(
        int userId,
        AIGenerateRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // 验证输入
        if (dto.Count > 20)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "同步生成最多支持20道题目，请使用异步生成接口",
                ErrorCode = "COUNT_EXCEEDED"
            };
        }

        if (string.IsNullOrWhiteSpace(dto.Subject))
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = "生成主题不能为空",
                ErrorCode = "INVALID_SUBJECT"
            };
        }

        // 获取用户的AI配置
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

        // 验证题库是否存在
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

        // 获取AI Provider
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

        // 构建AI请求
        var aiRequest = new AIQuestionGenerateRequest
        {
            Subject = dto.Subject,
            Count = dto.Count,
            Difficulty = dto.Difficulty,
            QuestionTypes = dto.QuestionTypes,
            Language = dto.Language,
            CustomPrompt = dto.CustomPrompt
        };

        // 调用AI生成（带重试机制）
        var apiKey = ExtractApiKeyFromConfig(dataSource.Config);
        var aiResponse = await CallAIWithRetryAsync(
            provider,
            apiKey,
            aiRequest,
            maxRetries: 3,
            cancellationToken);

        if (!aiResponse.Success || aiResponse.Questions.Count == 0)
        {
            return new AIGenerateResponseDto
            {
                Success = false,
                ErrorMessage = aiResponse.ErrorMessage ?? "AI生成失败",
                ErrorCode = aiResponse.ErrorCode ?? "AI_GENERATION_FAILED"
            };
        }

        // 保存生成的题目
        var savedQuestions = new List<GeneratedQuestionDto>();
        var questionBankId = dto.QuestionBankId ?? await CreateDefaultQuestionBankAsync(userId, cancellationToken);

        foreach (var question in aiResponse.Questions)
        {
            try
            {
                var questionEntity = new Question
                {
                    QuestionType = question.QuestionType,
                    QuestionText = question.QuestionText,
                    Options = JsonSerializer.Serialize(question.Options),
                    CorrectAnswer = question.CorrectAnswer,
                    Explanation = question.Explanation,
                    Difficulty = question.Difficulty,
                    QuestionBankId = questionBankId,
                    CreatedAt = DateTime.UtcNow
                };

                await _questionRepository.AddAsync(questionEntity, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存题目失败: {QuestionText}", question.QuestionText);
            }
        }

        return new AIGenerateResponseDto
        {
            Success = savedQuestions.Count > 0,
            Questions = savedQuestions,
            TokensUsed = aiResponse.TokensUsed,
            PartialSuccessCount = savedQuestions.Count < aiResponse.Questions.Count ? savedQuestions.Count : null
        };
    }

    public async Task<string> GenerateQuestionsAsyncAsync(
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
            Status = "pending",
            GeneratedCount = 0,
            TotalCount = dto.Count,
            CreatedAt = DateTime.UtcNow
        };

        lock (_taskLock)
        {
            _asyncTasks[taskId] = progress;
        }

        // 在后台线程中执行生成任务
        _ = Task.Run(async () => await ExecuteAsyncGeneration(taskId, userId, dto), cancellationToken);

        return taskId;
    }

    public async Task<AIGenerateProgressDto?> GetProgressAsync(
        int userId,
        string taskId,
        CancellationToken cancellationToken = default)
    {
        lock (_taskLock)
        {
            if (_asyncTasks.TryGetValue(taskId, out var progress))
            {
                // 返回副本避免外部修改
                return JsonSerializer.Deserialize<AIGenerateProgressDto>(
                    JsonSerializer.Serialize(progress));
            }
        }

        return null;
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

            // 调用同步生成逻辑
            var response = await GenerateQuestionsAsync(userId, dto, CancellationToken.None);

            if (response.Success)
            {
                UpdateTaskStatus(taskId, "completed", response.Questions);
            }
            else if (response.PartialSuccessCount.HasValue && response.PartialSuccessCount.Value > 0)
            {
                UpdateTaskStatus(taskId, "partial_success", response.Questions, response.ErrorMessage);
            }
            else
            {
                UpdateTaskStatus(taskId, "failed", errorMessage: response.ErrorMessage);
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

    /// <summary>
    /// 调用AI生成（带重试机制）
    /// </summary>
    private async Task<AIQuestionGenerateResponse> CallAIWithRetryAsync(
        IAIProvider provider,
        string apiKey,
        AIQuestionGenerateRequest request,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        var lastError = default(Exception);

        for (int retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                // 设置API Key（这里需要在provider中支持动态设置API Key）
                // 暂时使用provider内部实现
                var response = await provider.GenerateQuestionsAsync(request, cancellationToken);

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
    /// 从Config JSON中提取API Key
    /// </summary>
    private static string ExtractApiKeyFromConfig(string configJson)
    {
        try
        {
            if (string.IsNullOrEmpty(configJson))
            {
                return string.Empty;
            }

            var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(configJson);
            if (config != null && config.TryGetValue("apiKey", out var apiKeyElement))
            {
                return apiKeyElement.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
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

        return new GeneratedQuestionDto
        {
            Id = question.Id,
            QuestionType = question.QuestionType,
            QuestionText = question.QuestionText,
            Options = options,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            Difficulty = question.Difficulty,
            QuestionBankId = question.QuestionBankId,
            CreatedAt = question.CreatedAt
        };
    }
}
