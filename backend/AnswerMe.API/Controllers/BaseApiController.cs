using Microsoft.AspNetCore.Mvc;
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
}
