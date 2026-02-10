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
/// 集成测试数据库 Fixture
/// 使用 SQLite 内存数据库进行快速隔离测试
/// 提供完整的依赖注入配置,模拟真实应用环境
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly SqliteConnection _connection;
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _serviceProvider;

    public AnswerMeDbContext DbContext { get; private set; } = null!;
    public IServiceProvider Services => _serviceProvider;

    public DatabaseFixture()
    {
        // 创建 SQLite 内存数据库连接
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // 配置服务
        var services = new ServiceCollection();

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

        // 注册 Infrastructure 层服务
        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            options.UseSqlite(_connection);
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

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
    }

    public async Task InitializeAsync()
    {
        // 获取 DbContext
        var factory = _serviceProvider.GetRequiredService<IDbContextFactory<AnswerMeDbContext>>();
        DbContext = await factory.CreateDbContextAsync();

        // 确保数据库创建
        await DbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        _connection.Dispose();
        _scope.Dispose();
    }

    /// <summary>
    /// 清理所有表数据 (保持 Schema)
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        // 删除所有数据
        var tableNames = new[]
        {
            "AttemptDetails",
            "Attempts",
            "Questions",
            "QuestionBanks",
            "DataSources",
            "Users"
        };

        foreach (var table in tableNames)
        {
            await DbContext.Database.ExecuteSqlRawAsync($"DELETE FROM \"{table}\"");
        }

        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// 创建测试用户
    /// </summary>
    public async Task<User> CreateTestUserAsync(string? username = null)
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
    /// 创建测试题库
    /// </summary>
    public async Task<QuestionBank> CreateTestQuestionBankAsync(int userId, string? name = null)
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
    /// 创建测试题目
    /// </summary>
    public async Task<Question> CreateTestQuestionAsync(
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
    private QuestionData CreateDefaultQuestionData(QuestionType questionType)
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

    /// <summary>
    /// 批量创建测试题目 (用于数据迁移测试)
    /// </summary>
    public async Task<List<Question>> CreateTestQuestionsAsync(
        int questionBankId,
        int count,
        Random? random = null)
    {
        random ??= new Random(42); // 固定种子保证可重复性
        var questions = new List<Question>();
        var types = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse, QuestionType.FillBlank };

        for (int i = 0; i < count; i++)
        {
            var type = types[random.Next(types.Length)];
            var question = await CreateTestQuestionAsync(questionBankId, type);
            questions.Add(question);
        }

        return questions;
    }
}
