using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 基于 Redis 的 AI 生成任务进度存储实现
/// </summary>
public class RedisAIGenerationProgressStore : IAIGenerationProgressStore
{
    private const string ProgressKeyPrefix = "ai-gen:progress:";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisAIGenerationProgressStore> _logger;
    private readonly AIGenerationOptions _options;
    private readonly IDatabase _database;

    public RedisAIGenerationProgressStore(
        IConnectionMultiplexer redis,
        ILogger<RedisAIGenerationProgressStore> logger,
        IOptions<AIGenerationOptions> options)
    {
        _redis = redis;
        _logger = logger;
        _options = options.Value;
        _database = _redis.GetDatabase();
    }

    public async Task<AIGenerateProgressDto?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ProgressKeyPrefix}{taskId}";
            var jsonData = await _database.StringGetAsync(key);

            if (jsonData.IsNullOrEmpty)
            {
                return null;
            }

            var progress = System.Text.Json.JsonSerializer.Deserialize<AIGenerateProgressDto>(jsonData.ToString());
            return progress;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取任务进度失败: {TaskId}", taskId);
            return null;
        }
    }

    public async Task SetAsync(string taskId, AIGenerateProgressDto progress, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ProgressKeyPrefix}{taskId}";
            var jsonData = System.Text.Json.JsonSerializer.Serialize(progress);

            // 默认 TTL 为配置的 TaskTtlHours
            var expiration = ttl ?? TimeSpan.FromHours(_options.TaskTtlHours);
            await _database.StringSetAsync(key, jsonData, expiration);

            _logger.LogDebug("设置任务进度: {TaskId}, 状态: {Status}", taskId, progress.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置任务进度失败: {TaskId}", taskId);
            throw;
        }
    }

    public async Task UpdateAsync(string taskId, Action<AIGenerateProgressDto> updateAction, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ProgressKeyPrefix}{taskId}";
            var jsonData = await _database.StringGetAsync(key);

            if (jsonData.IsNullOrEmpty)
            {
                _logger.LogWarning("任务不存在，无法更新: {TaskId}", taskId);
                return;
            }

            var progress = System.Text.Json.JsonSerializer.Deserialize<AIGenerateProgressDto>(jsonData.ToString());
            if (progress == null)
            {
                _logger.LogWarning("任务数据解析失败: {TaskId}", taskId);
                return;
            }

            // 执行更新操作
            updateAction(progress);

            // 保存更新后的数据
            var updatedJsonData = System.Text.Json.JsonSerializer.Serialize(progress);
            await _database.StringSetAsync(key, updatedJsonData, TimeSpan.FromHours(_options.TaskTtlHours));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新任务进度失败: {TaskId}", taskId);
            throw;
        }
    }

    public async Task RemoveAsync(string taskId, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{ProgressKeyPrefix}{taskId}";
            await _database.KeyDeleteAsync(key);
            _logger.LogDebug("删除任务进度: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除任务进度失败: {TaskId}", taskId);
        }
    }
}
