using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 数据源管理控制器
/// </summary>
[Route("api/[controller]")]
[Authorize]
public class DataSourceController : BaseApiController
{
    private readonly IDataSourceService _dataSourceService;
    private readonly ILogger<DataSourceController> _logger;

    public DataSourceController(
        IDataSourceService dataSourceService,
        ILogger<DataSourceController> logger)
    {
        _dataSourceService = dataSourceService;
        _logger = logger;
    }

    /// <summary>
    /// 获取当前用户的数据源列表
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<DataSourceDto>>> GetDataSources(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dataSources = await _dataSourceService.GetUserDataSourcesAsync(userId, cancellationToken);
        return Ok(dataSources);
    }

    /// <summary>
    /// 根据ID获取数据源
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<DataSourceDto>> GetDataSource(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dataSource = await _dataSourceService.GetByIdAsync(id, userId, cancellationToken);

        if (dataSource == null)
        {
            return NotFoundWithError("数据源不存在");
        }

        return Ok(dataSource);
    }

    /// <summary>
    /// 获取用户的默认数据源
    /// </summary>
    [HttpGet("default")]
    public async Task<ActionResult<DataSourceDto>> GetDefault(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var dataSource = await _dataSourceService.GetDefaultAsync(userId, cancellationToken);

        if (dataSource == null)
        {
            return NotFoundWithError("未设置默认数据源");
        }

        return Ok(dataSource);
    }

    /// <summary>
    /// 创建新数据源
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<DataSourceDto>> Create(
        [FromBody] CreateDataSourceDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        try
        {
            var dataSource = await _dataSourceService.CreateAsync(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetDataSource), new { id = dataSource.Id }, dataSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建数据源失败");
            return InternalServerError("创建数据源失败", "CREATE_FAILED");
        }
    }

    /// <summary>
    /// 更新数据源
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<DataSourceDto>> Update(
        int id,
        [FromBody] UpdateDataSourceDto dto,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        var dataSource = await _dataSourceService.UpdateAsync(id, userId, dto, cancellationToken);

        if (dataSource == null)
        {
            return NotFoundWithError("数据源不存在");
        }

        return Ok(dataSource);
    }

    /// <summary>
    /// 删除数据源
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var success = await _dataSourceService.DeleteAsync(id, userId, cancellationToken);

        if (!success)
        {
            return NotFoundWithError("数据源不存在");
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 设置默认数据源
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<ActionResult> SetDefault(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var success = await _dataSourceService.SetDefaultAsync(id, userId, cancellationToken);

        if (!success)
        {
            return NotFoundWithError("数据源不存在");
        }

        return Ok(new { message = "已设置为默认数据源" });
    }

    /// <summary>
    /// 验证API密钥是否有效
    /// </summary>
    [HttpPost("{id}/validate")]
    public async Task<ActionResult> ValidateApiKey(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isValid = await _dataSourceService.ValidateApiKeyAsync(id, userId, cancellationToken);

        if (!isValid)
        {
            return BadRequestWithError("API密钥无效或数据源不存在");
        }

        return Ok(new { message = "API密钥有效", valid = true });
    }
}
