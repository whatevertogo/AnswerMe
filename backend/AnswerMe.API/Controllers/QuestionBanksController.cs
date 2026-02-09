using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 题库管理控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class QuestionBanksController : BaseApiController
{
    private readonly IQuestionBankService _questionBankService;
    private readonly IQuestionService _questionService;
    private readonly ILogger<QuestionBanksController> _logger;

    public QuestionBanksController(
        IQuestionBankService questionBankService,
        IQuestionService questionService,
        ILogger<QuestionBanksController> logger)
    {
        _questionBankService = questionBankService;
        _questionService = questionService;
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
            return BadRequestWithError("搜索关键词不能为空");
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
            return NotFoundWithError("题库不存在");
        }

        return Ok(questionBank);
    }

    /// <summary>
    /// 创建题库
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<QuestionBankDto>> Create([FromBody] CreateQuestionBankDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var questionBank = await _questionBankService.CreateAsync(userId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = questionBank.Id }, questionBank);
    }

    /// <summary>
    /// 更新题库
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<QuestionBankDto>> Update(int id, [FromBody] UpdateQuestionBankDto dto, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var questionBank = await _questionBankService.UpdateAsync(id, userId, dto, cancellationToken);

        if (questionBank == null)
        {
            return NotFoundWithError("题库不存在");
        }

        return Ok(questionBank);
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
            return NotFoundWithError("题库不存在");
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 导出题库为JSON
    /// </summary>
    [HttpGet("{id}/export")]
    public async Task<IActionResult> Export(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        // 获取题库详情
        var questionBank = await _questionBankService.GetByIdAsync(id, userId, cancellationToken);
        if (questionBank == null)
        {
            return NotFoundWithError("题库不存在");
        }

        // 获取题库的所有题目
        var questionsQuery = new QuestionListQueryDto
        {
            QuestionBankId = id,
            PageSize = 1000, // 获取最多1000题
            LastId = null
        };
        var questionsResult = await _questionService.GetListAsync(userId, questionsQuery, cancellationToken);

        // 构建导出数据
        var exportData = new
        {
            name = questionBank.Name,
            description = questionBank.Description,
            tags = questionBank.Tags,
            questionCount = questionsResult.TotalCount,
            questions = questionsResult.Data,
            exportedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            version = "0.1.0-alpha"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        var fileName = $"{questionBank.Name.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMddHHmmss}.json";

        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }
}
