using AnswerMe.Application.AI;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.AI;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddSingleton<IAIProvider, MinimaxProvider>();
        services.AddSingleton<IAIProvider, CustomApiProvider>();

        return services;
    }
}
