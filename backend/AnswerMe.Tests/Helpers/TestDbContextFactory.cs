using AnswerMe.Domain.Entities;
using AnswerMe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.Tests.Helpers;

/// <summary>
/// 测试用 DbContext 工厂
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// 创建内存数据库上下文
    /// </summary>
    public static AnswerMeDbContext CreateInMemoryContext(string? databaseName = null)
    {
        // 始终使用唯一的数据库实例，避免测试之间的数据污染
        var actualDatabaseName = databaseName != null
            ? $"{databaseName}_{Guid.NewGuid():N}"
            : Guid.NewGuid().ToString("N");

        var options = new DbContextOptionsBuilder<AnswerMeDbContext>()
            .UseInMemoryDatabase(actualDatabaseName)
            .Options;

        // 使用反射创建实例，绕过 required 成员检查
        // EF Core 会在运行时初始化 DbSet 属性
        var contextType = typeof(AnswerMeDbContext);
        var context = (AnswerMeDbContext)Activator.CreateInstance(
            contextType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic,
            null,
            new object[] { options },
            null)!;

        return context;
    }

    /// <summary>
    /// 创建带有测试数据的上下文
    /// </summary>
    public static async Task<AnswerMeDbContext> CreateWithSeedDataAsync(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);

        // 添加测试用户
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Users.AddAsync(user);

        // 添加测试题库
        var questionBank = new QuestionBank
        {
            Id = 1,
            Name = "测试题库",
            Description = "用于测试的题库",
            UserId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.QuestionBanks.AddAsync(questionBank);

        // 添加测试题目
        var questions = new List<Question>
        {
            new()
            {
                Id = 1,
                QuestionBankId = 1,
                QuestionText = "测试题目1",
                QuestionTypeEnum = Domain.Enums.QuestionType.SingleChoice,
                Difficulty = "medium",
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 2,
                QuestionBankId = 1,
                QuestionText = "测试题目2",
                QuestionTypeEnum = Domain.Enums.QuestionType.TrueFalse,
                Difficulty = "easy",
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 3,
                QuestionBankId = 1,
                QuestionText = "测试题目3",
                QuestionTypeEnum = Domain.Enums.QuestionType.FillBlank,
                Difficulty = "hard",
                OrderIndex = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
        await context.Questions.AddRangeAsync(questions);

        await context.SaveChangesAsync();
        return context;
    }
}
