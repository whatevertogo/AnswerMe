using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure;
using AnswerMe.Infrastructure.Repositories;
using AnswerMe.Application;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using AnswerMe.Application.AI;
using AnswerMe.Application.Authorization;
using Microsoft.Extensions.Configuration;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.UnitTests.Integration;

/// <summary>
/// API 集成测试的服务配置工厂
/// 提供与 Program.cs 相同的服务配置,但使用测试数据库
/// </summary>
public static class CustomWebApplicationFactory
{
    /// <summary>
    /// 创建配置好的 ServiceCollection (用于 SQLite 内存数据库)
    /// </summary>
    public static IServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        // 配置测试用 IConfiguration
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_TYPE"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
                ["JWT:Secret"] = "test-secret-key-must-be-at-least-32-characters-long-for-testing-only",
                ["JWT:Issuer"] = "AnswerMe-Test",
                ["JWT:Audience"] = "AnswerMeTestUsers",
                ["JWT:ExpiryDays"] = "30",
                ["LocalAuth:EnableLocalLogin"] = "false",
                ["ALLOWED_ORIGINS"] = "http://localhost:5173"
            })
            .Build();

        // 注册 Infrastructure 层
        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            options.UseSqlite("DataSource=:memory:");
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

        return services;
    }

    /// <summary>
    /// 创建配置好的 ServiceCollection (使用自定义连接字符串)
    /// </summary>
    public static IServiceCollection CreateServices(string connectionString)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_TYPE"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["JWT:Secret"] = "test-secret-key-must-be-at-least-32-characters-long-for-testing-only",
                ["JWT:Issuer"] = "AnswerMe-Test",
                ["JWT:Audience"] = "AnswerMeTestUsers"
            })
            .Build();

        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            options.UseSqlite(connectionString);
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

        return services;
    }

    /// <summary>
    /// 创建带外部数据库连接的 ServiceCollection (用于 TestContainers)
    /// </summary>
    public static IServiceCollection CreateServicesForExternalDatabase(
        string dbType,
        string connectionString)
    {
        var services = new ServiceCollection();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_TYPE"] = dbType,
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["JWT:Secret"] = "test-secret-key-must-be-at-least-32-characters-long-for-testing-only",
                ["JWT:Issuer"] = "AnswerMe-Test",
                ["JWT:Audience"] = "AnswerMeTestUsers"
            })
            .Build();

        // 根据数据库类型配置 DbContext
        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            if (dbType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // 注册所有服务
        services.AddHttpClient("AI", client => { client.Timeout = TimeSpan.FromSeconds(120); });
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IAttemptRepository, AttemptRepository>();
        services.AddScoped<IAttemptDetailRepository, AttemptDetailRepository>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IAIGenerationService, AIGenerationService>();
        services.AddSingleton<AIProviderFactory>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        return services;
    }
}
