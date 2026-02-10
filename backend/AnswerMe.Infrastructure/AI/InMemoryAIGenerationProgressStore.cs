using System.Collections.Concurrent;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 无 Redis 时的内存进度存储实现（单实例）
/// </summary>
public class InMemoryAIGenerationProgressStore : IAIGenerationProgressStore
{
    private readonly ConcurrentDictionary<string, AIGenerateProgressDto> _store = new();

    public Task<AIGenerateProgressDto?> GetAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(taskId, out var progress);
        return Task.FromResult(progress != null ? Clone(progress) : null);
    }

    public Task SetAsync(string taskId, AIGenerateProgressDto progress, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        _store[taskId] = Clone(progress);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string taskId, Action<AIGenerateProgressDto> updateAction, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(taskId, out var progress))
        {
            updateAction(progress);
        }
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(taskId, out _);
        return Task.CompletedTask;
    }

    private static AIGenerateProgressDto Clone(AIGenerateProgressDto source)
    {
        return new AIGenerateProgressDto
        {
            TaskId = source.TaskId,
            UserId = source.UserId,
            Status = source.Status,
            GeneratedCount = source.GeneratedCount,
            TotalCount = source.TotalCount,
            Questions = source.Questions,
            ErrorMessage = source.ErrorMessage,
            CreatedAt = source.CreatedAt,
            CompletedAt = source.CompletedAt
        };
    }
}
