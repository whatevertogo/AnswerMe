using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 统计数据控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class StatsController : BaseApiController
{
    private readonly IStatsService _statsService;
    private readonly ILogger<StatsController> _logger;

    public StatsController(
        IStatsService statsService,
        ILogger<StatsController> logger)
    {
        _statsService = statsService;
        _logger = logger;
    }

    /// <summary>
    /// 获取首页统计数据
    /// </summary>
    [HttpGet("home")]
    public async Task<ActionResult<HomeStatsDto>> GetHomeStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var stats = await _statsService.GetHomeStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取统计数据失败");
            return InternalServerError("获取统计数据失败", "GET_STATS_FAILED");
        }
    }

    /// <summary>
    /// 获取首页最近活动
    /// </summary>
    [HttpGet("recent-activities")]
    public async Task<ActionResult<List<HomeRecentActivityDto>>> GetHomeRecentActivities(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var safeLimit = Math.Clamp(limit, 1, 50);

        try
        {
            var activities = await _statsService.GetHomeRecentActivitiesAsync(userId, safeLimit, cancellationToken);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取最近活动失败");
            return InternalServerError("获取最近活动失败", "GET_RECENT_ACTIVITIES_FAILED");
        }
    }
}
