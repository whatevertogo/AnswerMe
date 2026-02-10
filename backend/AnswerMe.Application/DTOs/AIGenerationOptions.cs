namespace AnswerMe.Application.DTOs;

/// <summary>
/// AI 生成任务配置
/// </summary>
public class AIGenerationOptions
{
    public const string SectionName = "AIGeneration";

    /// <summary>
    /// 同步生成题目数量限制（超过此数量使用异步生成）
    /// </summary>
    public int MaxSyncCount { get; set; } = 20;

    /// <summary>
    /// 每批生成的题目数量
    /// </summary>
    public int BatchSize { get; set; } = 5;

    /// <summary>
    /// 任务状态过期时间（小时）
    /// </summary>
    public int TaskTtlHours { get; set; } = 24;

    /// <summary>
    /// 后台工作并发数
    /// </summary>
    public int WorkerConcurrency { get; set; } = 1;

    /// <summary>
    /// 队列轮询间隔（毫秒）
    /// </summary>
    public int QueuePollIntervalMs { get; set; } = 1000;
}
