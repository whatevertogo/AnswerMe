using Microsoft.AspNetCore.Mvc;
using AnswerMe.API.DTOs;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// API控制器基类，提供通用功能
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// 获取当前登录用户的ID
    /// </summary>
    /// <returns>用户ID</returns>
    /// <exception cref="UnauthorizedAccessException">当无法从JWT Token中解析用户ID时抛出</exception>
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("无效的用户身份");
        }
        return userId;
    }

    /// <summary>
    /// 返回标准化的错误响应（400 Bad Request）
    /// </summary>
    protected ActionResult BadRequestWithError(string message, string? errorCode = null)
    {
        var errorResponse = ErrorResponse.Create(message, 400, errorCode);
        return BadRequest(errorResponse);
    }

    /// <summary>
    /// 返回标准化的错误响应（404 Not Found）
    /// </summary>
    protected ActionResult NotFoundWithError(string message, string? errorCode = null)
    {
        var errorResponse = ErrorResponse.Create(message, 404, errorCode);
        return NotFound(errorResponse);
    }

    /// <summary>
    /// 返回标准化的错误响应（500 Internal Server Error）
    /// </summary>
    protected ActionResult InternalServerError(string message, string? errorCode = null)
    {
        var errorResponse = ErrorResponse.Create(message, 500, errorCode);
        return new ObjectResult(errorResponse) { StatusCode = 500 };
    }
}
