using AnswerMe.Application.Common;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using AnswerMe.Tests.Helpers;
using FluentAssertions;

namespace AnswerMe.Tests.Repositories;

/// <summary>
/// 题库仓储测试
/// </summary>
public class QuestionBankRepositoryTests : IAsyncLifetime
{
    private AnswerMeDbContext _context = null!;
    private QuestionBankRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _context = await TestDbContextFactory.CreateWithSeedDataAsync(nameof(QuestionBankRepositoryTests));
        _repository = new QuestionBankRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnQuestionBank()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("测试题库");
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
    public async Task GetByUserIdAsync_ShouldReturnUserBanks()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(1);

        // Assert
        result.Should().NotBeEmpty();
        result.All(qb => qb.UserId == 1).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdsAndUserIdAsync_MatchingIdsAndUser_ShouldReturnBanks()
    {
        // Arrange
        var ids = new List<int> { 1 };

        // Act
        var result = await _repository.GetByIdsAndUserIdAsync(ids, 1);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdsAndUserIdAsync_WrongUser_ShouldReturnEmpty()
    {
        // Arrange
        var ids = new List<int> { 1 };

        // Act
        var result = await _repository.GetByIdsAndUserIdAsync(ids, 999);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdsAndUserIdAsync_EmptyList_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.GetByIdsAndUserIdAsync(new List<int>(), 1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithSearchTerm_ShouldReturnMatchingBanks()
    {
        // Act
        var result = await _repository.SearchAsync(1, "测试");

        // Assert
        result.Should().NotBeEmpty();
        result.Count.Should().BeLessOrEqualTo(BatchLimits.MaxSearchResults);
    }

    [Fact]
    public async Task SearchAsync_NoMatch_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.SearchAsync(1, "不存在的关键词");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WrongUser_ShouldReturnEmpty()
    {
        // Act
        var result = await _repository.SearchAsync(999, "测试");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExistsByNameAsync_ExistingName_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.ExistsByNameAsync(1, "测试题库");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByNameAsync_NonExistingName_ShouldReturnFalse()
    {
        // Act
        var result = await _repository.ExistsByNameAsync(1, "不存在的题库");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetPagedAsync_FirstPage_ShouldReturnBanks()
    {
        // Act
        var result = await _repository.GetPagedAsync(1, 10, null);

        // Assert
        result.Should().NotBeEmpty();
        result.All(qb => qb.UserId == 1).Should().BeTrue();
    }
}
