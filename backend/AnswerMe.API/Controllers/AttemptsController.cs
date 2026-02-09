using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 答题管理控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class AttemptsController : BaseApiController
{
    private readonly IAttemptService _attemptService;
    private readonly ILogger<AttemptsController> _logger;

    public AttemptsController(
        IAttemptService attemptService,
        ILogger<AttemptsController> logger)
    {
        _attemptService = attemptService;
        _logger = logger;
    }

    /// <summary>
    /// 开始答题
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<StartAttemptResponseDto>> StartAttempt([FromBody] StartAttemptDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var result = await _attemptService.StartAttemptAsync(userId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始答题失败");
            return InternalServerError("开始答题失败", "START_FAILED");
        }
    }

    /// <summary>
    /// 提交单个答案
    /// </summary>
    [HttpPost("submit-answer")]
    public async Task<ActionResult<bool>> SubmitAnswer([FromBody] SubmitAnswerDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var result = await _attemptService.SubmitAnswerAsync(userId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交答案失败");
            return InternalServerError("提交答案失败", "SUBMIT_FAILED");
        }
    }

    /// <summary>
    /// 完成答题
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult<AttemptDto>> CompleteAttempt([FromBody] CompleteAttemptDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var result = await _attemptService.CompleteAttemptAsync(userId, dto, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成答题失败");
            return InternalServerError("完成答题失败", "COMPLETE_FAILED");
        }
    }

    /// <summary>
    /// 获取答题记录详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AttemptDto>> GetAttempt(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var attempt = await _attemptService.GetAttemptByIdAsync(id, userId, cancellationToken);
            if (attempt == null)
            {
                return NotFoundWithError("答题记录不存在");
            }
            return Ok(attempt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取答题记录失败");
            return InternalServerError("获取答题记录失败", "GET_FAILED");
        }
    }

    /// <summary>
    /// 获取答题记录的详情列表
    /// </summary>
    [HttpGet("{id}/details")]
    public async Task<ActionResult<List<AttemptDetailDto>>> GetAttemptDetails(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var details = await _attemptService.GetAttemptDetailsAsync(id, userId, cancellationToken);
            return Ok(details);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取答题详情失败");
            return InternalServerError("获取答题详情失败", "GET_DETAILS_FAILED");
        }
    }

    /// <summary>
    /// 获取用户的答题统计
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AttemptStatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var statistics = await _attemptService.GetStatisticsAsync(userId, cancellationToken);
            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取答题统计失败");
            return InternalServerError("获取答题统计失败", "GET_STATISTICS_FAILED");
        }
    }
}
