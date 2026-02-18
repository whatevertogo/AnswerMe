using AnswerMe.Application.Services;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using AnswerMe.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace AnswerMe.Tests.Services;

/// <summary>
/// 题目服务测试
/// </summary>
public class QuestionServiceTests : IAsyncLifetime
{
    private AnswerMeDbContext _context = null!;
    private IQuestionRepository _questionRepository = null!;
    private IQuestionBankRepository _questionBankRepository = null!;
    private QuestionService _service = null!;

    public async Task InitializeAsync()
    {
        _context = await TestDbContextFactory.CreateWithSeedDataAsync(nameof(QuestionServiceTests));
        _questionRepository = new QuestionRepository(_context);
        _questionBankRepository = new QuestionBankRepository(_context);
        _service = new QuestionService(_questionRepository, _questionBankRepository);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingQuestion_ShouldReturnDto()
    {
        // Act
        var result = await _service.GetByIdAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_ShouldReturnNull()
    {
        // Act
        var result = await _service.GetByIdAsync(1, 999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingQuestion_ShouldReturnTrue()
    {
        // Act
        var result = await _service.DeleteAsync(1, 1);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_ShouldReturnFalse()
    {
        // Act
        var result = await _service.DeleteAsync(1, 999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact(Skip = "InMemory 数据库不支持 ExecuteDeleteAsync，需要使用 SQLite 进行集成测试")]
    public async Task DeleteBatchAsync_AllAuthorized_ShouldDeleteAll()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3 };

        // Act
        var (successCount, notFoundCount) = await _service.DeleteBatchAsync(1, ids);

        // Assert
        successCount.Should().Be(3);
        notFoundCount.Should().Be(0);
    }

    [Fact(Skip = "InMemory 数据库不支持 ExecuteDeleteAsync，需要使用 SQLite 进行集成测试")]
    public async Task DeleteBatchAsync_PartiallyAuthorized_ShouldDeleteOnlyAuthorized()
    {
        // Arrange - 创建另一个用户的题库和题目
        var anotherUser = new Domain.Entities.User
        {
            Id = 2,
            Username = "otheruser",
            Email = "other@example.com",
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(anotherUser);

        var anotherBank = new Domain.Entities.QuestionBank
        {
            Id = 2,
            Name = "其他用户题库",
            UserId = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.QuestionBanks.Add(anotherBank);

        var anotherQuestion = new Domain.Entities.Question
        {
            Id = 10,
            QuestionBankId = 2,
            QuestionText = "其他用户题目",
            QuestionTypeEnum = QuestionType.SingleChoice,
            Difficulty = "medium",
            OrderIndex = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Questions.Add(anotherQuestion);
        await _context.SaveChangesAsync();

        var ids = new List<int> { 1, 10 }; // 1 是用户1的，10 是用户2的

        // Act
        var (successCount, notFoundCount) = await _service.DeleteBatchAsync(1, ids);

        // Assert
        successCount.Should().Be(1); // 只删除了用户1的题目
        notFoundCount.Should().Be(1); // 用户2的题目算作无权限
    }

    [Fact]
    public async Task DeleteBatchAsync_EmptyList_ShouldReturnZero()
    {
        // Act
        var (successCount, notFoundCount) = await _service.DeleteBatchAsync(1, new List<int>());

        // Assert
        successCount.Should().Be(0);
        notFoundCount.Should().Be(0);
    }

    [Fact(Skip = "InMemory 数据库不支持 ExecuteDeleteAsync，需要使用 SQLite 进行集成测试")]
    public async Task DeleteBatchAsync_NonExistingIds_ShouldReturnAllNotFound()
    {
        // Arrange
        var ids = new List<int> { 999, 1000, 1001 };

        // Act
        var (successCount, notFoundCount) = await _service.DeleteBatchAsync(1, ids);

        // Assert
        successCount.Should().Be(0);
        notFoundCount.Should().Be(3);
    }

    [Fact]
    public async Task SearchAsync_AuthorizedBank_ShouldReturnResults()
    {
        // Act
        var result = await _service.SearchAsync(1, 1, "测试");

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_UnauthorizedBank_ShouldThrow()
    {
        // Act & Assert
        var act = async () => await _service.SearchAsync(999, 1, "测试");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("题库不存在或无权访问");
    }
}
