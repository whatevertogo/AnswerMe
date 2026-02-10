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
}
