using System.Text;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using AnswerMe.Application.AI;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Infrastructure.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog结构化日志
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "AnswerMe.API")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/answerme-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 7)
    .CreateLogger();

builder.Host.UseSerilog();

// 配置DbContext - 根据环境变量选择数据库
var dbType = builder.Configuration.GetValue<string>("DB_TYPE") ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AnswerMeDbContext>(options =>
{
    if (dbType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
        Log.Information("使用PostgreSQL数据库: {ConnectionString}", connectionString);
    }
    else
    {
        // 开发环境默认使用SQLite
        options.UseSqlite(connectionString);
        Log.Information("使用SQLite数据库: {ConnectionString}", connectionString);

        // 输出当前工作目录和数据库路径
        var currentDir = Directory.GetCurrentDirectory();
        var dbPath = Path.Combine(currentDir, "answerme_dev.db");
        Log.Information("当前工作目录: {CurrentDir}", currentDir);
        Log.Information("预期数据库路径: {DbPath}", dbPath);
        Log.Information("数据库文件是否存在: {Exists}", File.Exists(dbPath));
    }

    // 启用开发环境下的敏感数据日志记录（仅开发环境）
#if DEBUG
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
#endif
});

// 配置JWT认证
var jwtSecretFromConfig = builder.Configuration.GetValue<string>("JWT:Secret");
var jwtSecretFromEnv = builder.Configuration.GetValue<string>("JWT_SECRET");

// 优先使用环境变量，否则使用配置文件
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

var jwtSettings = new JwtSettings(
    builder.Configuration.GetValue<string>("JWT:Issuer", "AnswerMe"),
    builder.Configuration.GetValue<string>("JWT:Audience", "AnswerMeUsers"),
    jwtSecret,
    builder.Configuration.GetValue<int>("JWT:ExpiryDays", 30)
);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddSingleton(jwtSettings);

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
builder.Services.AddDataProtection();

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
});

// 注册服务
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDataSourceRepository, DataSourceRepository>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();
builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();
builder.Services.AddScoped<IAIGenerationService, AIGenerationService>();

// 注册AI Providers
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAIProvider, OpenAIProvider>();
builder.Services.AddSingleton<IAIProvider, QwenProvider>();
builder.Services.AddSingleton<IAIProvider, ZhipuProvider>();
builder.Services.AddSingleton<IAIProvider, MinimaxProvider>();
builder.Services.AddSingleton<AIProviderFactory>();

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
