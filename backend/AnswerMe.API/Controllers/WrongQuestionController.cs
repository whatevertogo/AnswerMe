using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 错题本和学习分析控制器
/// </summary>
[Route("api/wrong-questions")]
[Authorize]
public class WrongQuestionController : BaseApiController
{
    private readonly IAttemptService _attemptService;
    private readonly ILogger<WrongQuestionController> _logger;

    public WrongQuestionController(
        IAttemptService attemptService,
        ILogger<WrongQuestionController> logger)
    {
        _attemptService = attemptService;
        _logger = logger;
    }

    /// <summary>
    /// 获取错题列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<WrongQuestionListDto>> GetWrongQuestions(
        [FromQuery] int? questionBankId,
        [FromQuery] string? questionType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        try
        {
            var query = new WrongQuestionQueryDto
            {
                QuestionBankId = questionBankId,
                QuestionType = questionType,
                StartDate = startDate,
                EndDate = endDate,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
            var result = await _attemptService.GetWrongQuestionsAsync(userId, query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取错题列表失败");
            return InternalServerError("获取错题列表失败", "GET_WRONG_QUESTIONS_FAILED");
        }
    }

    /// <summary>
    /// 标记错题为已掌握
    /// </summary>
    [HttpPost("{id}/master")]
    public async Task<ActionResult> MarkAsMastered(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _attemptService.MarkQuestionAsMasteredAsync(userId, id, cancellationToken);
            return Ok(new { success = true, message = "标记成功" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestWithError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记错题失败");
            return InternalServerError("标记错题失败", "MARK_MASTERED_FAILED");
        }
    }

    /// <summary>
    /// 获取学习统计数据
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<LearningStatsDto>> GetLearningStats(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var stats = await _attemptService.GetLearningStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取学习统计失败");
            return InternalServerError("获取学习统计失败", "GET_LEARNING_STATS_FAILED");
        }
    }
}
