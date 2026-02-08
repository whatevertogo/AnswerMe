using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 题库管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuestionBanksController : ControllerBase
{
    private readonly IQuestionBankService _questionBankService;
    private readonly ILogger<QuestionBanksController> _logger;

    public QuestionBanksController(
        IQuestionBankService questionBankService,
        ILogger<QuestionBanksController> logger)
    {
        _questionBankService = questionBankService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题库列表（游标分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<QuestionBankListDto>> GetList([FromQuery] QuestionBankListQueryDto query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _questionBankService.GetListAsync(userId, query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 搜索题库
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<QuestionBankDto>>> Search([FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new { message = "搜索关键词不能为空" });
        }

        var userId = GetCurrentUserId();
        var results = await _questionBankService.SearchAsync(userId, searchTerm, cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// 根据ID获取题库
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionBankDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var questionBank = await _questionBankService.GetByIdAsync(id, userId, cancellationToken);

        if (questionBank == null)
        {
            return NotFound(new { message = "题库不存在" });
        }

        return Ok(questionBank);
    }

    /// <summary>
    /// 创建题库
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuestionBankDto>> Create([FromBody] CreateQuestionBankDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var questionBank = await _questionBankService.CreateAsync(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = questionBank.Id }, questionBank);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题库失败");
            return BadRequest(new { message = "创建题库失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新题库
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionBankDto>> Update(int id, [FromBody] UpdateQuestionBankDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var questionBank = await _questionBankService.UpdateAsync(id, userId, dto, cancellationToken);

            if (questionBank == null)
            {
                return NotFound(new { message = "题库不存在" });
            }

            return Ok(questionBank);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新题库失败");
            return BadRequest(new { message = "更新题库失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除题库
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var success = await _questionBankService.DeleteAsync(id, userId, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "题库不存在" });
        }

        return Ok(new { message = "删除成功" });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("无法获取用户ID");
    }
}
