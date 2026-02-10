using System.Collections.Concurrent;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 无 Redis 时的内存任务队列实现（单实例）
/// </summary>
public class InMemoryAIGenerationTaskQueue : IAIGenerationTaskQueue
{
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly ConcurrentDictionary<string, (int userId, AIGenerateRequestDto request)> _taskData = new();

    public Task EnqueueAsync(string taskId, int userId, AIGenerateRequestDto request, CancellationToken cancellationToken = default)
    {
        _taskData[taskId] = (userId, request);
        _queue.Enqueue(taskId);
        return Task.CompletedTask;
    }

    public Task<(string taskId, int userId, AIGenerateRequestDto request)?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        if (!_queue.TryDequeue(out var taskId))
        {
            return Task.FromResult<(string taskId, int userId, AIGenerateRequestDto request)?>(null);
        }

        if (!_taskData.TryGetValue(taskId, out var data))
        {
            return Task.FromResult<(string taskId, int userId, AIGenerateRequestDto request)?>(null);
        }

        return Task.FromResult<(string taskId, int userId, AIGenerateRequestDto request)?>((taskId, data.userId, data.request));
    }

    public Task CompleteTaskAsync(string taskId, CancellationToken cancellationToken = default)
    {
        _taskData.TryRemove(taskId, out _);
        return Task.CompletedTask;
    }

    public Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_queue.Count);
    }
}
