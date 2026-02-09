using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AnswerMe.API.DTOs;
using System.Security.Claims;

namespace AnswerMe.API.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// 检查请求是否来自本地（仅用于本地登录验证）
    /// </summary>
    private bool IsLocalRequest()
    {
        var connection = HttpContext.Connection;
        var remoteIp = connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            return false;
        }

        // 检查是否为本地回环地址
        if (System.Net.IPAddress.IsLoopback(remoteIp))
        {
            return true;
        }

        // 检查 X-Forwarded-For 头（代理场景）
        if (HttpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            var forwardedIp = forwardedFor.FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp) &&
                System.Net.IPAddress.TryParse(forwardedIp, out var parsedIp) &&
                System.Net.IPAddress.IsLoopback(parsedIp))
            {
                return true;
            }
        }

        _logger.LogWarning("本地登录请求被拒绝，远程IP: {RemoteIp}", remoteIp);
        return false;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="registerDto">注册信息</param>
    /// <returns>认证响应</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { Message = ex.Message, StatusCode = 400 });
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="loginDto">登录信息</param>
    /// <returns>认证响应</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorResponse { Message = ex.Message, StatusCode = 401 });
        }
    }

    /// <summary>
    /// 本地模式登录（无需凭据，适用于个人部署）
    /// </summary>
    /// <returns>认证响应</returns>
    [HttpPost("local-login")]
    public async Task<ActionResult<AuthResponseDto>> LocalLogin()
    {
        // P1-2修复：仅允许本地IP访问
        if (!IsLocalRequest())
        {
            _logger.LogWarning("非本地IP尝试访问本地登录端点");
            return StatusCode(403, new ErrorResponse { Message = "本地登录仅允许从本机访问", StatusCode = 403 });
        }

        try
        {
            var result = await _authService.LocalLoginAsync();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse { Message = ex.Message, StatusCode = 400 });
        }
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>用户信息</returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ErrorResponse { Message = "无效的Token", StatusCode = 401 });
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound(new ErrorResponse { Message = "用户不存在", StatusCode = 404 });
        }

        return Ok(user);
    }
}
