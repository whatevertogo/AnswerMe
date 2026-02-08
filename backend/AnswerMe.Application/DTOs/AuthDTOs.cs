using AnswerMe.Domain.Entities;

namespace AnswerMe.Application.DTOs;

/// <summary>
/// 用户注册DTO
/// </summary>
public record RegisterDto
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// 用户登录DTO
/// </summary>
public record LoginDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// 认证响应DTO
/// </summary>
public record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;
    public UserDto User { get; init; } = null!;
}

/// <summary>
/// 用户DTO
/// </summary>
public record UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
