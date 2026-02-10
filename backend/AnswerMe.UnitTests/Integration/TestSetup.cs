using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure;
using AnswerMe.Infrastructure.Repositories;
using AnswerMe.Application;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using AnswerMe.Application.AI;
using AnswerMe.Application.Authorization;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.UnitTests.Integration;

/// <summary>
/// 集成测试基类
/// 提供通用的测试设置和辅助方法
/// 使用 SQLite 内存数据库,每个测试类拥有独立的数据库实例
/// </summary>
public abstract class TestBase : IAsyncLifetime
{
    protected readonly SqliteConnection Connection;
    protected AnswerMeDbContext DbContext = null!;
    protected IServiceScope Scope = null!;
    protected IServiceProvider ServiceProvider = null!;

    // 仓储和服务实例 (供子类使用)
    protected IUserRepository UserRepository = null!;
    protected IQuestionBankRepository QuestionBankRepository = null!;
    protected IQuestionRepository QuestionRepository = null!;
    protected IQuestionService QuestionService = null!;
    protected IQuestionBankService QuestionBankService = null!;

    protected TestBase()
    {
        // 创建独立的 SQLite 内存数据库连接
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();
    }

    public virtual async Task InitializeAsync()
    {
        // 配置服务
        var services = new ServiceCollection();

        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();
        Scope = ServiceProvider.CreateScope();

        // 获取 DbContext
        var factory = ServiceProvider.GetRequiredService<IDbContextFactory<AnswerMeDbContext>>();
        DbContext = await factory.CreateDbContextAsync();

        // 确保数据库创建
        await DbContext.Database.EnsureCreatedAsync();

        // 初始化仓储和服务实例
        UserRepository = ServiceProvider.GetRequiredService<IUserRepository>();
        QuestionBankRepository = ServiceProvider.GetRequiredService<IQuestionBankRepository>();
        QuestionRepository = ServiceProvider.GetRequiredService<IQuestionRepository>();
        QuestionService = ServiceProvider.GetRequiredService<IQuestionService>();
        QuestionBankService = ServiceProvider.GetRequiredService<IQuestionBankService>();
    }

    public virtual async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        Scope.Dispose();
        Connection.Dispose();
    }

    /// <summary>
    /// 配置依赖注入服务
    /// 子类可以重写此方法添加额外的服务
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // 配置测试用 IConfiguration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_TYPE"] = "Sqlite",
                ["JWT:Secret"] = "test-secret-key-must-be-at-least-32-characters-long",
                ["JWT:Issuer"] = "AnswerMe-Test",
                ["JWT:Audience"] = "AnswerMeTestUsers",
                ["LocalAuth:EnableLocalLogin"] = "false"
            })
            .Build();

        // 注册 DbContext
        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            options.UseSqlite(Connection);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // 注册 Infrastructure 仓储和 AI Providers
        services.AddHttpClient("AI", client => { client.Timeout = TimeSpan.FromSeconds(120); });
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IAttemptRepository, AttemptRepository>();
        services.AddScoped<IAttemptDetailRepository, AttemptDetailRepository>();

        // 注册 Application 层服务
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IAIGenerationService, AIGenerationService>();
        services.AddSingleton<AIProviderFactory>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
    }

    /// <summary>
    /// 清理数据库中的所有数据 (保持 Schema)
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        var tables = new[]
        {
            "AttemptDetails", "Attempts", "Questions",
            "QuestionBanks", "DataSources", "Users"
        };

        foreach (var table in tables)
        {
            await DbContext.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"");
        }

        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 在事务中执行操作并自动回滚
    /// 用于测试不污染数据库的操作
    /// </summary>
    protected async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> action)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();

        try
        {
            var result = await action();
            await transaction.RollbackAsync();
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 在事务中执行操作并自动回滚 (无返回值)
    /// </summary>
    protected async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        await using var transaction = await DbContext.Database.BeginTransactionAsync();

        try
        {
            await action();
            await transaction.RollbackAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 创建测试用户 (辅助方法)
    /// </summary>
    protected async Task<User> CreateTestUserAsync(string? username = null)
    {
        var user = new User
        {
            Username = username ?? $"testuser_{Guid.NewGuid()}",
            Email = $"test_{Guid.NewGuid()}@example.com",
            PasswordHash = "test_hash",
            CreatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// 创建测试题库 (辅助方法)
    /// </summary>
    protected async Task<QuestionBank> CreateTestQuestionBankAsync(int userId, string? name = null)
    {
        var questionBank = new QuestionBank
        {
            UserId = userId,
            Name = name ?? $"TestBank{Guid.NewGuid():N}",
            Description = "Test description"
        };

        DbContext.QuestionBanks.Add(questionBank);
        await DbContext.SaveChangesAsync();

        return questionBank;
    }

    /// <summary>
    /// 创建测试题目 (辅助方法)
    /// </summary>
    protected async Task<Question> CreateTestQuestionAsync(
        int questionBankId,
        QuestionType questionType,
        QuestionData? data = null)
    {
        var question = new Question
        {
            QuestionBankId = questionBankId,
            QuestionText = $"Test {questionType} question",
            QuestionTypeEnum = questionType,
            Data = data ?? CreateDefaultQuestionData(questionType),
            Explanation = "Test explanation",
            Difficulty = "medium",
            OrderIndex = 0
        };

        DbContext.Questions.Add(question);
        await DbContext.SaveChangesAsync();

        return question;
    }

    /// <summary>
    /// 创建默认的题目数据
    /// </summary>
    protected QuestionData CreateDefaultQuestionData(QuestionType questionType)
    {
        return questionType switch
        {
            QuestionType.SingleChoice => new ChoiceQuestionData
            {
                Options = new List<string> { "A. Option 1", "B. Option 2", "C. Option 3", "D. Option 4" },
                CorrectAnswers = new List<string> { "A" },
                Explanation = "Single choice explanation"
            },
            QuestionType.MultipleChoice => new ChoiceQuestionData
            {
                Options = new List<string> { "A. Option 1", "B. Option 2", "C. Option 3", "D. Option 4" },
                CorrectAnswers = new List<string> { "A", "C" },
                Explanation = "Multiple choice explanation"
            },
            QuestionType.TrueFalse => new BooleanQuestionData
            {
                CorrectAnswer = true,
                Explanation = "True/false explanation"
            },
            QuestionType.FillBlank => new FillBlankQuestionData
            {
                AcceptableAnswers = new List<string> { "Answer1", "Answer2" },
                Explanation = "Fill blank explanation"
            },
            QuestionType.ShortAnswer => new ShortAnswerQuestionData
            {
                ReferenceAnswer = "This is a reference answer",
                Explanation = "Short answer explanation"
            },
            _ => throw new ArgumentOutOfRangeException(nameof(questionType), $"Unknown question type: {questionType}")
        };
    }
}
