using System.Text;
using AspNetCoreRateLimit;
using AnswerMe.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AnswerMe.Application;
using AnswerMe.Application.DTOs;
using AnswerMe.Infrastructure;
using AnswerMe.Infrastructure.Data;
using AnswerMe.API.BackgroundServices;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog结构化日志
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AnswerMe.API")
    // 在 appsettings.json 中配置过滤规则
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/answerme-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();
builder.Services.AddFluentValidationAutoValidation();

// 配置 AI 生成选项
builder.Services.Configure<AIGenerationOptions>(builder.Configuration.GetSection(AIGenerationOptions.SectionName));

// 注册 AI 生成后台服务（需要先检查 Redis 是否配置且可连接）
var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConnectionString))
{
    try
    {
        // 验证 Redis 连接
        var redis = ConnectionMultiplexer.Connect(redisConnectionString);
        if (!redis.IsConnected)
        {
            throw new InvalidOperationException("Redis 连接失败");
        }
        redis.Dispose();

        builder.Services.AddHostedService<AIGenerationWorker>();
        Log.Information("AI 生成后台服务已注册，Redis 连接成功");
    }
    catch (Exception ex)
    {
        // 开发环境只警告，生产环境阻止启动
        if (builder.Environment.IsDevelopment())
        {
            Log.Warning(ex, "无法连接到 Redis ({ConnectionString})，AI 异步生成功能将不可用", redisConnectionString);
            Log.Warning("开发模式：后端将继续启动，但 AI 异步生成功能禁用");
            Log.Warning("要启用 AI 异步生成，请运行: docker-compose up -d redis");
        }
        else
        {
            Log.Fatal(ex, "无法连接到 Redis ({ConnectionString})，请先启动 Redis 服务", redisConnectionString);
            Log.Fatal("后端启动中止。AI 异步生成功能需要 Redis 支持。");
            throw new InvalidOperationException("Redis 连接失败，后端无法启动。请确保 Redis 服务已启动。", ex);
        }
    }
}
else
{
    Log.Warning("Redis 未配置，AI 生成后台服务未注册，异步生成功能将不可用");
}

// 配置JWT认证
var jwtSecret = GetJwtSecret(builder.Configuration);
var jwtSettings = CreateJwtSettings(builder.Configuration, jwtSecret);

builder.Services.Configure<JwtSettings>(options =>
{
    options.Issuer = jwtSettings.Issuer;
    options.Audience = jwtSettings.Audience;
    options.Secret = jwtSettings.Secret;
    options.ExpiryDays = jwtSettings.ExpiryDays;
});
builder.Services.AddSingleton(jwtSettings);

// 配置本地模式认证
builder.Services.Configure<LocalAuthSettings>(builder.Configuration.GetSection(LocalAuthSettings.SectionName));
Log.Information("本地登录模式: {Enabled}", builder.Configuration.GetValue<bool>("LocalAuth:EnableLocalLogin"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.Secret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 配置Data Protection（用于加密API密钥）
var keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), "keys");

// 确保keys目录存在
if (!Directory.Exists(keysDirectory))
{
    try
    {
        Directory.CreateDirectory(keysDirectory);
        Log.Information("创建Data Protection密钥目录: {KeysDirectory}", keysDirectory);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "无法创建keys目录，Data Protection将使用临时密钥");
    }
}

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("AnswerMe")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

Log.Information("Data Protection密钥将持久化到: {KeysDirectory}", keysDirectory);

// 配置CORS
var allowedOrigins = builder.Configuration.GetValue<string>("ALLOWED_ORIGINS") ?? "http://localhost:3000,http://localhost:5173";
var originList = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(originList)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 配置速率限制
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddInMemoryRateLimiting();

// 添加控制器
builder.Services.AddControllers(options =>
{
    // 注册全局异常处理过滤器
    options.Filters.Add<AnswerMe.API.Filters.GlobalExceptionFilter>();
})
.AddJsonOptions(options =>
{
    // 使用 camelCase 命名策略，与前端 JavaScript 保持一致
    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    // 配置 DateTime 序列化为 ISO 8601 格式（前端兼容）
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// 添加API浏览器
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// 启用CORS
app.UseCors("AllowSpecificOrigins");

// 启用速率限制
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// 自动应用数据库迁移（开发环境）
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AnswerMeDbContext>();
    try
    {
        // 等待数据库文件初始化
        Thread.Sleep(500);
        var pendingMigrations = db.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            Log.Information("发现 {Count} 个待应用的迁移，开始应用...", pendingMigrations.Count());
            db.Database.Migrate();
            Log.Information("数据库迁移应用成功");
        }
        else
        {
            Log.Information("数据库已是最新状态");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "数据库迁移失败");
    }

    // 数据一致性检查（开发环境默认启用）
    var enableConsistencyCheck = builder.Configuration.GetValue<bool>("DataConsistency:EnableCheck", true);
    if (enableConsistencyCheck)
    {
        Log.Information("数据一致性检查已启用，开始执行...");

        try
        {
            var checkService = scope.ServiceProvider.GetRequiredService<AnswerMe.Infrastructure.Services.DataConsistencyCheckService>();
            var report = await checkService.CheckAllQuestionsAsync();

            Log.Information(
                "数据一致性检查完成: 总题数 {Total}, 不一致数 {Inconsistent}, 一致率 {Rate:F2}%",
                report.TotalQuestions,
                report.InconsistentQuestions,
                report.ConsistencyRate);

            if (report.Issues.Count > 0)
            {
                var errorCount = report.Issues.Count(i => i.Severity == "Error");
                var warningCount = report.Issues.Count(i => i.Severity == "Warning");

                Log.Warning(
                    "发现 {ErrorCount} 个错误, {WarningCount} 个警告",
                    errorCount,
                    warningCount);

                foreach (var issue in report.Issues.Take(10))
                {
                    var logLevel = issue.Severity == "Error" ? Serilog.Events.LogEventLevel.Error : Serilog.Events.LogEventLevel.Warning;
                    Log.Write(logLevel,
                        "题目 {QuestionId}: {Type} - {Description}. 建议: {Recommendation}",
                        issue.QuestionId,
                        issue.IssueType,
                        issue.Description,
                        issue.Recommendation);
                }

                if (report.Issues.Count > 10)
                {
                    Log.Warning("还有 {Remaining} 个问题未显示", report.Issues.Count - 10);
                }
            }

            if (report.IsHealthy)
            {
                Log.Information("✅ 所有题目数据一致性检查通过");
            }
            else if (report.ConsistencyRate >= 99.5)
            {
                Log.Warning("⚠️ 数据一致性率 {Rate:F2}% 未达到 100%，但接近目标", report.ConsistencyRate);
            }
            else
            {
                Log.Error("❌ 数据一致性率 {Rate:F2}% 低于目标 99.5%，请执行数据修复", report.ConsistencyRate);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据一致性检查失败");
        }
    }
}

// 健康检查端点
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        application = "AnswerMe API"
    });
})
.WithName("HealthCheck");

try
{
    Log.Information("启动 AnswerMe API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "应用启动失败");
}
finally
{
    Log.CloseAndFlush();
}

// 辅助方法
static string GetJwtSecret(IConfiguration configuration)
{
    var jwtSecretFromConfig = configuration.GetValue<string>("JWT:Secret");
    var jwtSecretFromEnv = configuration.GetValue<string>("JWT_SECRET");
    var jwtSecret = !string.IsNullOrEmpty(jwtSecretFromEnv) ? jwtSecretFromEnv : jwtSecretFromConfig;

    if (string.IsNullOrEmpty(jwtSecret))
    {
        throw new InvalidOperationException(
            "JWT密钥未配置。请设置环境变量 JWT_SECRET(至少32个字符)或在配置文件中设置JWT:Secret");
    }

    if (jwtSecret.Length < 32)
    {
        throw new InvalidOperationException(
            "JWT密钥长度不足。环境变量 JWT_SECRET 必须至少包含32个字符以保证安全性");
    }

    return jwtSecret;
}

static JwtSettings CreateJwtSettings(IConfiguration configuration, string jwtSecret)
{
    return new JwtSettings(
        configuration.GetValue<string>("JWT:Issuer", "AnswerMe"),
        configuration.GetValue<string>("JWT:Audience", "AnswerMeUsers"),
        jwtSecret,
        configuration.GetValue<int>("JWT:ExpiryDays", 30)
    );
}
