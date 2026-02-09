using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// AI生成题目控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class AIGenerationController : BaseApiController
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
                return BadRequestWithError(result.ErrorMessage ?? "生成题目失败", result.ErrorCode);
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "生成题目失败");
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成题目时发生未预期错误");
            return InternalServerError("服务器内部错误，请稍后重试", "INTERNAL_ERROR");
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

        if (dto.Count < 20)
        {
            return BadRequestWithError("异步生成适用于≥20题的场景，<20题请使用同步生成接口");
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("用户 {UserId} 请求异步生成题目: Subject={Subject}, Count={Count}",
            userId, dto.Subject, dto.Count);

        try
        {
            var taskId = await _aiGenerationService.StartAsyncGeneration(userId, dto, cancellationToken);

            return Ok(new AIGenerateResponseDto
            {
                Success = true,
                TaskId = taskId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建异步生成任务失败");
            return InternalServerError("服务器内部错误，请稍后重试", "INTERNAL_ERROR");
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
                return NotFoundWithError("任务不存在");
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询生成进度失败: TaskId={TaskId}", taskId);
            return InternalServerError("服务器内部错误，请稍后重试", "INTERNAL_ERROR");
        }
    }
}
