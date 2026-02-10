using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnswerMe.API.BackgroundServices;

/// <summary>
/// AI 生成任务后台处理服务
/// 从 Redis 队列中取出任务并执行生成
/// </summary>
public class AIGenerationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAIGenerationTaskQueue _taskQueue;
    private readonly IAIGenerationProgressStore _progressStore;
    private readonly ILogger<AIGenerationWorker> _logger;
    private readonly AIGenerationOptions _options;

    public AIGenerationWorker(
        IServiceProvider serviceProvider,
        IAIGenerationTaskQueue taskQueue,
        IAIGenerationProgressStore progressStore,
        ILogger<AIGenerationWorker> logger,
        IOptions<AIGenerationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _taskQueue = taskQueue;
        _progressStore = progressStore;
        _logger = logger;
        _options = options.Value;
    }

    // 追踪正在执行的任务数量
    private int _runningTasks = 0;
    private readonly object _taskLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI 生成后台服务启动");

        // 使用 SemaphoreSlim 控制并发数
        using var concurrencySemaphore = new SemaphoreSlim(_options.WorkerConcurrency, _options.WorkerConcurrency);

        // 主循环
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 从队列取出任务
                var task = await _taskQueue.DequeueAsync(stoppingToken);

                if (task == null)
                {
                    // 队列为空，等待一段时间后重试
                    await Task.Delay(_options.QueuePollIntervalMs, stoppingToken);
                    continue;
                }

                var (taskId, userId, request) = task.Value;

                _logger.LogInformation("开始处理任务: {TaskId}, 用户: {UserId}", taskId, userId);

                // 等待并发槽位
                await concurrencySemaphore.WaitAsync(stoppingToken);

                // 记录任务开始
                lock (_taskLock)
                {
                    _runningTasks++;
                }

                // 在后台线程中处理任务（不阻塞主循环）
                _ = ProcessTaskWithCleanupAsync(taskId, userId, request, concurrencySemaphore, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // 正常退出
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "后台服务主循环异常");
                await Task.Delay(_options.QueuePollIntervalMs, stoppingToken);
            }
        }

        // 等待所有正在执行的任务完成
        _logger.LogInformation("等待所有任务完成...");
        await WaitForAllTasksToCompleteAsync(stoppingToken);

        _logger.LogInformation("AI 生成后台服务停止");
    }

    /// <summary>
    /// 等待所有正在执行的任务完成
    /// </summary>
    private async Task WaitForAllTasksToCompleteAsync(CancellationToken cancellationToken)
    {
        var lastLogged = DateTime.UtcNow;
        var logInterval = TimeSpan.FromSeconds(5);

        while (true)
        {
            int currentRunning;
            lock (_taskLock)
            {
                currentRunning = _runningTasks;
            }

            if (currentRunning == 0)
            {
                _logger.LogInformation("所有任务已完成");
                break;
            }

            // 定期记录等待状态
            if (DateTime.UtcNow - lastLogged > logInterval)
            {
                _logger.LogInformation("等待 {Count} 个任务完成...", currentRunning);
                lastLogged = DateTime.UtcNow;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    /// <summary>
    /// 处理任务并确保清理资源
    /// </summary>
    private async Task ProcessTaskWithCleanupAsync(
        string taskId,
        int userId,
        AIGenerateRequestDto request,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        var shouldCleanup = false;
        try
        {
            shouldCleanup = await ProcessTaskAsync(taskId, userId, request, cancellationToken);
        }
        finally
        {
            // 仅在任务完成/部分成功后清理队列任务数据，失败状态保留以便排查
            if (shouldCleanup)
            {
                await _taskQueue.CompleteTaskAsync(taskId, cancellationToken);
            }
            // 释放并发槽位
            semaphore.Release();
            // 记录任务完成
            lock (_taskLock)
            {
                _runningTasks--;
            }
        }
    }

    private async Task<bool> ProcessTaskAsync(string taskId, int userId, AIGenerateRequestDto request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var aiGenerationService = scope.ServiceProvider.GetRequiredService<IAIGenerationService>();

        try
        {
            // 更新状态为处理中
            await _progressStore.UpdateAsync(taskId, progress =>
            {
                progress.Status = "processing";
            }, cancellationToken);

            // 执行生成（通过 IAIGenerationService 的内部方法）
            await aiGenerationService.ExecuteTaskAsync(taskId, userId, request, UpdateProgressAsync, cancellationToken);

            var progress = await _progressStore.GetAsync(taskId, cancellationToken);
            var finalStatus = progress?.Status ?? "unknown";
            var shouldCleanup = finalStatus != "failed";

            _logger.LogInformation("任务结束: {TaskId}, 状态: {Status}", taskId, finalStatus);
            return shouldCleanup;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "任务执行失败: {TaskId}", taskId);

            // 更新状态为失败
            await _progressStore.UpdateAsync(taskId, progress =>
            {
                progress.Status = "failed";
                progress.ErrorMessage = ex.Message;
                progress.CompletedAt = DateTime.UtcNow;
            }, cancellationToken);
            return false;
        }
    }

    private async Task UpdateProgressAsync(string taskId, int generatedCount, int totalCount, string status)
    {
        try
        {
            await _progressStore.UpdateAsync(taskId, progress =>
            {
                progress.GeneratedCount = generatedCount;
                if (!string.IsNullOrEmpty(status))
                {
                    progress.Status = status;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新进度失败: {TaskId}", taskId);
        }
    }
}
