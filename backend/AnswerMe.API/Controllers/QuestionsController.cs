using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 题目管理控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class QuestionsController : BaseApiController
{
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionService questionService,
        ILogger<QuestionsController> logger)
    {
        _questionService = questionService;
        _logger = logger;
    }

    /// <summary>
    /// 获取题目列表(游标分页)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<QuestionListDto>> GetList([FromQuery] QuestionListQueryDto query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        try
        {
            var result = await _questionService.GetListAsync(userId, query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取题目列表失败");
            return StatusCode(500, new { message = "获取题目列表失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 搜索题目
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<QuestionDto>>> Search([FromQuery] int questionBankId, [FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequest(new { message = "搜索关键词不能为空" });
        }

        var userId = GetCurrentUserId();
        try
        {
            var results = await _questionService.SearchAsync(questionBankId, userId, searchTerm, cancellationToken);
            return Ok(results);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索题目失败");
            return StatusCode(500, new { message = "搜索题目失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 根据ID获取题目
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<QuestionDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var question = await _questionService.GetByIdAsync(id, userId, cancellationToken);

        if (question == null)
        {
            return NotFound(new { message = "题目不存在" });
        }

        return Ok(question);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] CreateQuestionDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var question = await _questionService.CreateAsync(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建题目失败");
            return BadRequest(new { message = "创建题目失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionDto>> Update(int id, [FromBody] UpdateQuestionDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var question = await _questionService.UpdateAsync(id, userId, dto, cancellationToken);

            if (question == null)
            {
                return NotFound(new { message = "题目不存在" });
            }

            return Ok(question);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新题目失败");
            return BadRequest(new { message = "更新题目失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 删除题目
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var success = await _questionService.DeleteAsync(id, userId, cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "题目不存在" });
        }

        return Ok(new { message = "删除成功" });
    }
}
