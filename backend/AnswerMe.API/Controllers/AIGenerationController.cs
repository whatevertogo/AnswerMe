using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// AI生成题目控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIGenerationController : ControllerBase
{
    private readonly IAIGenerationService _aiGenerationService;
    private readonly ILogger<AIGenerationController> _logger;

    public AIGenerationController(
        IAIGenerationService aiGenerationService,
        ILogger<AIGenerationController> logger)
    {
        _aiGenerationService = aiGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// 生成题目（同步，≤20题）
    /// </summary>
    /// <param name="dto">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的题目</returns>
    [HttpPost("generate")]
    public async Task<ActionResult<AIGenerateResponseDto>> Generate(
        [FromBody] AIGenerateRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("用户 {UserId} 请求生成题目: Subject={Subject}, Count={Count}",
            userId, dto.Subject, dto.Count);

        try
        {
            var result = await _aiGenerationService.GenerateQuestionsAsync(userId, dto, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    message = result.ErrorMessage,
                    errorCode = result.ErrorCode
                });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "生成题目失败");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成题目时发生未预期错误");
            return StatusCode(500, new { message = "服务器内部错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 生成题目（异步，>20题）
    /// </summary>
    /// <param name="dto">生成请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务ID</returns>
    [HttpPost("generate-async")]
    public async Task<ActionResult<AIGenerateResponseDto>> GenerateAsync(
        [FromBody] AIGenerateRequestDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (dto.Count <= 20)
        {
            return BadRequest(new
            {
                message = "异步生成适用于>20题的场景，≤20题请使用同步生成接口"
            });
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("用户 {UserId} 请求异步生成题目: Subject={Subject}, Count={Count}",
            userId, dto.Subject, dto.Count);

        try
        {
            var taskId = await _aiGenerationService.GenerateQuestionsAsyncAsync(userId, dto, cancellationToken);

            return Ok(new AIGenerateResponseDto
            {
                Success = true,
                TaskId = taskId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建异步生成任务失败");
            return StatusCode(500, new { message = "服务器内部错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 查询生成进度
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成进度</returns>
    [HttpGet("progress/{taskId}")]
    public async Task<ActionResult<AIGenerateProgressDto>> GetProgress(
        string taskId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        try
        {
            var progress = await _aiGenerationService.GetProgressAsync(userId, taskId, cancellationToken);

            if (progress == null)
            {
                return NotFound(new { message = "任务不存在" });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询生成进度失败: TaskId={TaskId}", taskId);
            return StatusCode(500, new { message = "服务器内部错误，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无效的用户身份");
        }
        return userId;
    }
}
