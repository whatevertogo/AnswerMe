using AnswerMe.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnswerMe.API.BackgroundServices;

/// <summary>
/// 应用启动时执行数据一致性检查的后台服务
/// </summary>
public class DataConsistencyStartupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataConsistencyStartupService> _logger;

    public DataConsistencyStartupService(
        IServiceProvider serviceProvider,
        ILogger<DataConsistencyStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 只在启动时执行一次检查
        await CheckDataConsistencyAsync(stoppingToken);

        // 检查完成后结束当前后台服务（不影响主应用）
        _logger.LogInformation("数据一致性检查完成，后台服务结束");
    }

    private async Task CheckDataConsistencyAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var checkService = scope.ServiceProvider.GetRequiredService<DataConsistencyCheckService>();

        _logger.LogInformation("开始执行数据一致性检查...");

        try
        {
            var report = await checkService.CheckAllQuestionsAsync(cancellationToken);

            _logger.LogInformation(
                "数据一致性检查完成: 总题数 {Total}, 不一致数 {Inconsistent}, 一致率 {Rate:F2}%",
                report.TotalQuestions,
                report.InconsistentQuestions,
                report.ConsistencyRate);

            if (report.Issues.Count > 0)
            {
                var errorCount = report.Issues.Count(i => i.Severity == "Error");
                var warningCount = report.Issues.Count(i => i.Severity == "Warning");

                _logger.LogWarning(
                    "发现 {ErrorCount} 个错误, {WarningCount} 个警告",
                    errorCount,
                    warningCount);

                // 记录前 10 个问题的详细信息
                foreach (var issue in report.Issues.Take(10))
                {
                    var logLevel = issue.Severity == "Error" ? LogLevel.Error : LogLevel.Warning;
                    _logger.Log(logLevel,
                        "题目 {QuestionId}: {Type} - {Description}. 建议: {Recommendation}",
                        issue.QuestionId,
                        issue.IssueType,
                        issue.Description,
                        issue.Recommendation);
                }

                if (report.Issues.Count > 10)
                {
                    _logger.LogWarning("还有 {Remaining} 个问题未显示", report.Issues.Count - 10);
                }
            }

            if (report.IsHealthy)
            {
                _logger.LogInformation("✅ 所有题目数据一致性检查通过");
            }
            else if (report.ConsistencyRate >= 99.5)
            {
                _logger.LogWarning("⚠️ 数据一致性率 {Rate:F2}% 未达到 100%，但接近目标", report.ConsistencyRate);
            }
            else
            {
                _logger.LogError("❌ 数据一致性率 {Rate:F2}% 低于目标 99.5%，请执行数据修复", report.ConsistencyRate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据一致性检查失败");
        }
    }
}
