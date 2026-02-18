using AnswerMe.Application.Common;
using AnswerMe.Domain.Enums;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using AnswerMe.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Tests.Repositories;

/// <summary>
/// 题目仓储测试
/// </summary>
public class QuestionRepositoryTests : IAsyncLifetime
{
    private AnswerMeDbContext _context = null!;
    private QuestionRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _context = await TestDbContextFactory.CreateWithSeedDataAsync(nameof(QuestionRepositoryTests));
        _repository = new QuestionRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnQuestion()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.QuestionText.Should().Be("测试题目1");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_MultipleIds_ShouldReturnAllMatchingQuestions()
    {
        // Arrange
        var ids = new List<int> { 1, 2, 3 };

        // Act
        var result = await _repository.GetByIdsAsync(ids);

        // Assert
        result.Should().HaveCount(3);
        result.Select(q => q.Id).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task GetByIdsAsync_EmptyList_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByIdsAsync(new List<int>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAsync_NullList_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByIdsAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact(Skip = "InMemory 数据库不支持 ExecuteDeleteAsync，需要使用 SQLite 进行集成测试")]
    public async Task DeleteRangeAsync_ExistingIds_ShouldDeleteAndReturnCount()
    {
        // Arrange
        var ids = new List<int> { 1, 2 };

        // Act
        var result = await _repository.DeleteRangeAsync(ids);

        // Assert
        result.Should().Be(2);

        // Verify deletion
        var remaining = await _context.Questions.CountAsync();
        remaining.Should().Be(1); // 原来有3个，删除了2个
    }

    [Fact]
    public async Task DeleteRangeAsync_EmptyList_ShouldReturnZero()
    {
        // Act
        var result = await _repository.DeleteRangeAsync(new List<int>());

        // Assert
        result.Should().Be(0);
    }

    [Fact(Skip = "InMemory 数据库不支持 ExecuteDeleteAsync，需要使用 SQLite 进行集成测试")]
    public async Task DeleteRangeAsync_NonExistingIds_ShouldReturnZero()
    {
        // Act
        var result = await _repository.DeleteRangeAsync(new List<int> { 999, 1000 });

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ShouldLimitResults()
    {
        // Act
        var result = await _repository.SearchAsync(1, "测试");

        // Assert
        result.Count.Should().BeLessOrEqualTo(BatchLimits.MaxSearchResults);
    }

    [Fact]
    public async Task SearchAsync_EmptySearchTerm_ShouldLimitResults()
    {
        // Act
        var result = await _repository.SearchAsync(1, "");

        // Assert
        result.Count.Should().BeLessOrEqualTo(BatchLimits.MaxSearchResults);
    }

    [Fact]
    public async Task CountByQuestionBankIdAsync_ShouldReturnCorrectCount()
    {
        // Act
        var result = await _repository.CountByQuestionBankIdAsync(1);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task CountByQuestionBankIdsAsync_MultipleBanks_ShouldReturnCorrectCounts()
    {
        // Act
        var result = await _repository.CountByQuestionBankIdsAsync(new List<int> { 1 });

        // Assert
        result.Should().ContainKey(1);
        result[1].Should().Be(3);
    }

    [Fact]
    public async Task GetPagedAsync_FirstPage_ShouldReturnCorrectNumber()
    {
        // Arrange
        var pageSize = 2;

        // Act
        var result = await _repository.GetPagedAsync(1, pageSize, null);

        // Assert
        result.Should().HaveCount(pageSize + 1); // 多取一个判断是否有更多
    }

    [Fact]
    public async Task AddAsync_NewQuestion_ShouldAddSuccessfully()
    {
        // Arrange
        var question = new Domain.Entities.Question
        {
            QuestionBankId = 1,
            QuestionText = "新测试题目",
            QuestionTypeEnum = QuestionType.ShortAnswer,
            Difficulty = "medium",
            OrderIndex = 4,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repository.AddAsync(question);
        await _repository.SaveChangesAsync();

        // Assert
        result.Id.Should().BeGreaterThan(0);
        var saved = await _repository.GetByIdAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.QuestionText.Should().Be("新测试题目");
    }
}
