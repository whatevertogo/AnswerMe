using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AnswerMe.Application.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly JwtSettings _jwtSettings;
    private readonly LocalAuthSettings _localAuthSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IOptions<JwtSettings> jwtSettings,
        IOptions<LocalAuthSettings> localAuthSettings,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtSettings = jwtSettings.Value;
        _localAuthSettings = localAuthSettings.Value;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
    {
        // 检查邮箱是否已存在
        if (await _userRepository.EmailExistsAsync(registerDto.Email, cancellationToken))
        {
            throw new InvalidOperationException("邮箱已被注册");
        }

        // 检查用户名是否已存在
        if (await _userRepository.UsernameExistsAsync(registerDto.Username, cancellationToken))
        {
            throw new InvalidOperationException("用户名已被使用");
        }

        // 创建新用户
        var user = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);

        // 生成JWT Token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
    {
        // 查找用户
        var user = await _userRepository.GetByEmailAsync(loginDto.Email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("邮箱或密码错误");
        }

        // 验证密码
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("邮箱或密码错误");
        }

        // 生成JWT Token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(user)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<AuthResponseDto> LocalLoginAsync(CancellationToken cancellationToken = default)
    {
        if (!_localAuthSettings.EnableLocalLogin)
        {
            throw new InvalidOperationException("本地登录模式未启用");
        }

        // 尝试获取或创建本地用户
        User? localUser = await _userRepository.GetByEmailAsync(_localAuthSettings.DefaultEmail, cancellationToken);

        if (localUser == null)
        {
            _logger.LogInformation("创建默认本地用户: {Username}", _localAuthSettings.DefaultUsername);

            // 创建本地用户
            localUser = new User
            {
                Username = _localAuthSettings.DefaultUsername,
                Email = _localAuthSettings.DefaultEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(_localAuthSettings.DefaultPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(localUser, cancellationToken);
            _logger.LogInformation("本地用户创建成功: UserId={UserId}", localUser.Id);
        }
        else
        {
            _logger.LogInformation("本地用户登录: UserId={UserId}, Username={Username}", localUser.Id, localUser.Username);
        }

        // 生成JWT Token
        var token = GenerateJwtToken(localUser);

        return new AuthResponseDto
        {
            Token = token,
            User = MapToUserDto(localUser)
        };
    }

    /// <summary>
    /// 生成JWT Token
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(_jwtSettings.ExpiryDays),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 映射User到UserDto
    /// </summary>
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
