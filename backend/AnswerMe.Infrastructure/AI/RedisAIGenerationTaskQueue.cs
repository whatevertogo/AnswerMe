using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 基于 Redis 的 AI 生成任务队列实现
/// </summary>
public class RedisAIGenerationTaskQueue : IAIGenerationTaskQueue
{
    private const string QueueKeyPrefix = "ai-gen:queue";
    private const string TaskDataKeyPrefix = "ai-gen:task:data:";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAIGenerationTaskQueue> _logger;
    private readonly AIGenerationOptions _options;
    private readonly IDatabase _database;

    public RedisAIGenerationTaskQueue(
        IConnectionMultiplexer redis,
        ILogger<RedisAIGenerationTaskQueue> logger,
        IOptions<AIGenerationOptions> options)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
        _database = _redis.GetDatabase();
    }

    public async Task EnqueueAsync(string taskId, int userId, AIGenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            // 存储任务数据（序列化为 JSON）
            var taskData = new TaskQueueData
            {
                TaskId = taskId,
                UserId = userId,
                Request = request
            };

            var jsonData = System.Text.Json.JsonSerializer.Serialize(taskData);
            var dataKey = $"{TaskDataKeyPrefix}{taskId}";

            // 设置任务数据，过期时间为 TTL 的 2 倍（确保任务完成前数据不丢失）
            var ttl = TimeSpan.FromHours(_options.TaskTtlHours * 2);
            await _database.StringSetAsync(dataKey, jsonData, ttl);

            // 将任务 ID 加入队列
            await _database.ListLeftPushAsync(QueueKeyPrefix, taskId);

            _logger.LogInformation("任务已加入队列: {TaskId}, 用户: {UserId}", taskId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加入队列失败: {TaskId}", taskId);
            throw;
        }
    }

    public async Task<(string taskId, int userId, AIGenerateRequestDto request)?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 从队列右侧取出任务 ID（FIFO）
            var taskId = await _database.ListRightPopAsync(QueueKeyPrefix);

            if (taskId.IsNullOrEmpty)
            {
                return null;
            }

            // 获取任务数据
            var dataKey = $"{TaskDataKeyPrefix}{taskId}";
            var jsonData = await _database.StringGetAsync(dataKey);

            if (jsonData.IsNullOrEmpty)
            {
                _logger.LogWarning("任务数据不存在: {TaskId}", taskId);
                return null;
            }

            var taskData = System.Text.Json.JsonSerializer.Deserialize<TaskQueueData>(jsonData.ToString());
            if (taskData == null)
            {
                _logger.LogWarning("任务数据解析失败: {TaskId}", taskId);
                return null;
            }

            // 不再立即删除任务数据，而是在任务完成后删除
            // 这样可以在任务失败时重新入队

            return (taskData.TaskId, taskData.UserId, taskData.Request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从队列取出任务失败");
            return null;
        }
    }

    public async Task CompleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var dataKey = $"{TaskDataKeyPrefix}{taskId}";
            await _database.KeyDeleteAsync(dataKey);
            _logger.LogInformation("任务数据已清理: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理任务数据失败: {TaskId}", taskId);
        }
    }

    public async Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return (int)await _database.ListLengthAsync(QueueKeyPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取队列长度失败");
            return 0;
        }
    }

    /// <summary>
    /// 队列任务数据
    /// </summary>
    private class TaskQueueData
    {
        public string TaskId { get; set; } = string.Empty;
        public int UserId { get; set; }
        public AIGenerateRequestDto Request { get; set; } = new();
    }
}
