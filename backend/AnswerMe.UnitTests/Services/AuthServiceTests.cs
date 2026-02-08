using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnswerMe.UnitTests.Services;

/// <summary>
/// AuthService 单元测试
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly AuthService _authService;
    private readonly JwtSettings _jwtSettings;

    public AuthServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _jwtSettings = new JwtSettings("AnswerMe", "AnswerMeUsers",
            "test_jwt_secret_key_must_be_at_least_32_characters_long_for_security", 30);
        _authService = new AuthService(_mockUserRepo.Object,
            Options.Create(_jwtSettings));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password123!"
        };
        _mockUserRepo
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _authService.RegisterAsync(dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("邮箱已被注册");
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnToken()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password123!"
        };
        _mockUserRepo
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepo
            .Setup(r => r.UsernameExistsAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken ct) =>
            {
                user.Id = 1;
                return user;
            });

        // Act
        var result = await _authService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(dto.Email);
        result.User.Username.Should().Be(dto.Username);
    }

    [Fact]
    public async Task RegisterAsync_ShouldHashPassword()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Username = "testuser",
            Email = "test@test.com",
            Password = "Password123!"
        };
        _mockUserRepo
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserRepo
            .Setup(r => r.UsernameExistsAsync(dto.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        User? capturedUser = null;
        _mockUserRepo
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, ct) => capturedUser = u)
            .ReturnsAsync((User user, CancellationToken ct) =>
            {
                user.Id = 1;
                return user;
            });

        // Act
        await _authService.RegisterAsync(dto);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.PasswordHash.Should().NotBe(dto.Password);
        capturedUser.PasswordHash.Should().StartWith("$2a$"); // BCrypt hash prefix
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowException()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "nonexistent@test.com",
            Password = "Password123!"
        };
        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _authService.LoginAsync(dto);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        exception.Message.Should().Contain("邮箱或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowException()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@test.com",
            Password = "WrongPassword123!"
        };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        };

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _authService.LoginAsync(dto);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
        exception.Message.Should().Contain("邮箱或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var dto = new LoginDto
        {
            Email = "test@test.com",
            Password = "Password123!"
        };
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _mockUserRepo
            .Setup(r => r.GetByEmailAsync(dto.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be(user.Email);
        result.User.Username.Should().Be(user.Username);
    }
}
