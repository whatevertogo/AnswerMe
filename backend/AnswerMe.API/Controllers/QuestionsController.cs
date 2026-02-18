using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.Common;
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
        var result = await _questionService.GetListAsync(userId, query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 搜索题目
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<QuestionDto>>> Search([FromQuery] int questionBankId, [FromQuery] string searchTerm, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return BadRequestWithError("搜索关键词不能为空");
        }

        var userId = GetCurrentUserId();
        var results = await _questionService.SearchAsync(questionBankId, userId, searchTerm, cancellationToken);
        return Ok(results);
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
            return NotFoundWithError("题目不存在");
        }

        return Ok(question);
    }

    /// <summary>
    /// 创建题目
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuestionDto>> Create([FromBody] CreateQuestionDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var question = await _questionService.CreateAsync(userId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = question.Id }, question);
    }

    /// <summary>
    /// 更新题目
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionDto>> Update(int id, [FromBody] UpdateQuestionDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var question = await _questionService.UpdateAsync(id, userId, dto, cancellationToken);

        if (question == null)
        {
            return NotFoundWithError("题目不存在");
        }

        return Ok(question);
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
            return NotFoundWithError("题目不存在");
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 批量创建题目
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<List<QuestionDto>>> CreateBatch([FromBody] List<CreateQuestionDto> dtos, CancellationToken cancellationToken)
    {
        if (dtos == null || dtos.Count == 0)
        {
            return BadRequestWithError("题目列表不能为空");
        }

        if (dtos.Count > BatchLimits.MaxBatchCreateSize)
        {
            return BadRequestWithError($"单次最多创建 {BatchLimits.MaxBatchCreateSize} 道题目，当前: {dtos.Count}");
        }

        var userId = GetCurrentUserId();
        var questions = await _questionService.CreateBatchAsync(userId, dtos, cancellationToken);
        return Ok(questions);
    }

    /// <summary>
    /// 批量删除题目
    /// </summary>
    [HttpPost("batch-delete")]
    public async Task<ActionResult> DeleteBatch([FromBody] List<int> ids, CancellationToken cancellationToken)
    {
        if (ids == null || ids.Count == 0)
        {
            return BadRequestWithError("题目ID列表不能为空");
        }

        if (ids.Count > BatchLimits.MaxBatchDeleteSize)
        {
            return BadRequestWithError($"单次最多删除 {BatchLimits.MaxBatchDeleteSize} 道题目，当前: {ids.Count}");
        }

        var userId = GetCurrentUserId();
        var (successCount, notFoundCount) = await _questionService.DeleteBatchAsync(userId, ids, cancellationToken);

        return Ok(new
        {
            message = $"成功删除 {successCount} 道题目" + (notFoundCount > 0 ? $", {notFoundCount} 道题目不存在或无权删除" : ""),
            successCount,
            notFoundCount
        });
    }
}
