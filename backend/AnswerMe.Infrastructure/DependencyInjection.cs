using AnswerMe.Application.AI;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.AI;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AnswerMe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dbType = configuration.GetValue<string>("DB_TYPE") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AnswerMeDbContext>(options =>
        {
            if (dbType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }

            // ✅ 修复P0-3: 生产环境禁用敏感数据日志记录
            // 仅在显式启用详细日志时才启用（通过环境变量 ENABLE_SENSITIVE_LOGGING=true）
#if DEBUG
            var enableSensitiveLogging = configuration.GetValue<bool>("ENABLE_SENSITIVE_LOGGING", false);
            if (enableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
#endif
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDataSourceRepository, DataSourceRepository>();
        services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IAttemptRepository, AttemptRepository>();
        services.AddScoped<IAttemptDetailRepository, AttemptDetailRepository>();

        services.AddHttpClient("AI", client => { client.Timeout = TimeSpan.FromSeconds(180); });
        services.AddSingleton<IAIProvider, OpenAIProvider>();
        services.AddSingleton<IAIProvider, DeepSeekProvider>();
        services.AddSingleton<IAIProvider, QwenProvider>();
        services.AddSingleton<IAIProvider, ZhipuProvider>();
        services.AddSingleton<IAIProvider>(sp =>
            new MinimaxProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<MinimaxProvider>>(),
                "minimax_cn",
                "https://api.minimaxi.com/v1/text/chatcompletion_v2"));
        services.AddSingleton<IAIProvider>(sp =>
            new MinimaxProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<MinimaxProvider>>(),
                "minimax_global",
                "https://api.minimax.io/v1/text/chatcompletion_v2"));
        services.AddSingleton<IAIProvider>(sp =>
            new MinimaxProvider(
                sp.GetRequiredService<IHttpClientFactory>(),
                sp.GetRequiredService<ILogger<MinimaxProvider>>(),
                "minimax",
                "https://api.minimaxi.com/v1/text/chatcompletion_v2"));
        services.AddSingleton<IAIProvider, CustomApiProvider>();

        // Redis 配置（如果配置了连接字符串）
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);
                configurationOptions.AbortOnConnectFail = false;
                configurationOptions.ConnectRetry = 3;
                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            services.AddSingleton<IAIGenerationTaskQueue, RedisAIGenerationTaskQueue>();
            services.AddSingleton<IAIGenerationProgressStore, RedisAIGenerationProgressStore>();
        }
        else
        {
            // Redis 未配置时回退为内存实现（仅适用于单实例开发环境）
            services.AddSingleton<IAIGenerationTaskQueue, InMemoryAIGenerationTaskQueue>();
            services.AddSingleton<IAIGenerationProgressStore, InMemoryAIGenerationProgressStore>();
        }

        return services;
    }
}
