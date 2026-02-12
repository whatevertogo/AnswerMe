using AnswerMe.Application.AI;
using AnswerMe.Application.Authorization;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerMe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDataSourceService, DataSourceService>();
        services.AddScoped<IQuestionBankService, QuestionBankService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IAttemptInsightService, AttemptInsightService>();
        services.AddScoped<IAIGenerationService, AIGenerationService>();
        services.AddScoped<IStatsService, StatsService>();

        services.AddSingleton<AIProviderFactory>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

        // TODO: P1 - 添加 AutoMapper (需要先安装包)
        // services.AddAutoMapper(typeof(MappingProfile));

        return services;
    }
}

