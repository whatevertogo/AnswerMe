using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using AnswerMe.Infrastructure.Data;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace AnswerMe.UnitTests.Integration;

/// <summary>
/// 数据迁移性能测试
/// 验证大数据量迁移、批量操作和一致性检查的性能表现
/// </summary>
[Trait("Category", "Performance")]
public class DataMigrationPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public DataMigrationPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region 大数据量迁移性能测试

    /// <summary>
    /// 迁移 1000 条旧格式题目应在 5 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task Migration_1000Questions_ShouldCompleteWithin5Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateOldFormatQuestions(1000);
        
        await using var context = CreateSqliteContext();
        await context.Database.EnsureCreatedAsync();
        
        // Act
        var sw = Stopwatch.StartNew();
        await MigrateQuestionsInBatchesAsync(context, questions, batchSize: 100);
        sw.Stop();
        
        // Assert
        _output.WriteLine($"迁移 1000 条题目耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(5000, "迁移 1000 条旧格式题目应在 5 秒内完成");
    }

    /// <summary>
    /// 迁移 5000 条旧格式题目应在 20 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task Migration_5000Questions_ShouldCompleteWithin20Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateOldFormatQuestions(5000);
        
        await using var context = CreateSqliteContext();
        await context.Database.EnsureCreatedAsync();
        
        // Act
        var sw = Stopwatch.StartNew();
        await MigrateQuestionsInBatchesAsync(context, questions, batchSize: 200);
        sw.Stop();
        
        // Assert
        _output.WriteLine($"迁移 5000 条题目耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(20000, "迁移 5000 条旧格式题目应在 20 秒内完成");
    }

    /// <summary>
    /// 迁移 10000 条旧格式题目应在 45 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task Migration_10000Questions_ShouldCompleteWithin45Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateOldFormatQuestions(10000);
        
        await using var context = CreateSqliteContext();
        await context.Database.EnsureCreatedAsync();
        
        // Act
        var sw = Stopwatch.StartNew();
        await MigrateQuestionsInBatchesAsync(context, questions, batchSize: 500);
        sw.Stop();
        
        // Assert
        _output.WriteLine($"迁移 10000 条题目耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(45000, "迁移 10000 条旧格式题目应在 45 秒内完成");
    }

    #endregion

    #region 内存使用性能测试

    /// <summary>
    /// 迁移过程中内存增长不应超过 100MB
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task Migration_5000Questions_MemoryShouldNotExceed100MB()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateOldFormatQuestions(5000);
        
        // 强制 GC 收集，获取基准内存
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(true);
        
        await using var context = CreateSqliteContext();
        await context.Database.EnsureCreatedAsync();
        
        // Act
        await MigrateQuestionsInBatchesAsync(context, questions, batchSize: 200);
        
        var peakMemory = GC.GetTotalMemory(false);
        
        // Assert
        var memoryUsed = peakMemory - baselineMemory;
        _output.WriteLine($"内存使用: {(memoryUsed / 1024 / 1024):F2}MB");
        memoryUsed.Should().BeLessThan(100 * 1024 * 1024, "迁移过程中内存增长不应超过 100MB");
    }

    /// <summary>
    /// 迁移完成后内存应正常回收
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task Migration_5000Questions_MemoryShouldBeCollected()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateOldFormatQuestions(5000);
        
        // Act
        for (int i = 0; i < 3; i++)
        {
            await using var context = CreateSqliteContext();
            await context.Database.EnsureCreatedAsync();
            await MigrateQuestionsInBatchesAsync(context, questions, batchSize: 200);
        }
        
        // 强制 GC 收集
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var finalMemory = GC.GetTotalMemory(true);
        
        // Assert
        _output.WriteLine($"最终内存使用: {(finalMemory / 1024 / 1024):F2}MB");
        // 内存应该回到合理水平
        finalMemory.Should().BeLessThan(200 * 1024 * 1024, "迁移完成后内存应正常回收");
    }

    #endregion

    #region 批量操作性能测试

    /// <summary>
    /// 批量创建 1000 条题目应在 10 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task BulkCreate_1000Questions_ShouldCompleteWithin10Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateNewFormatQuestions(1000);
        
        await using var context = CreateSqliteContext();
        await context.Database.EnsureCreatedAsync();
        
        // Act
        var sw = Stopwatch.StartNew();
        
        context.Questions.AddRange(questions);
        await context.SaveChangesAsync();
        
        sw.Stop();
        
        // Assert
        _output.WriteLine($"批量创建 1000 条题目耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(10000, "批量创建 1000 条题目应在 10 秒内完成");
    }

    /// <summary>
    /// 批量查询 1000 条题目应在 1 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task BulkQuery_1000Questions_ShouldCompleteWithin1Second()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateNewFormatQuestions(1000);
        
        {
            await using var setupCtx = CreateSqliteContext();
            await setupCtx.Database.EnsureCreatedAsync();
            setupCtx.Questions.AddRange(questions);
            await setupCtx.SaveChangesAsync();
        }
        
        await using var queryCtx = CreateSqliteContext();
        
        // Act
        var sw = Stopwatch.StartNew();
        var result = await queryCtx.Questions.ToListAsync();
        sw.Stop();
        
        // Assert
        _output.WriteLine($"批量查询 1000 条题目耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "批量查询 1000 条题目应在 1 秒内完成");
    }

    /// <summary>
    /// 分页查询性能测试（10000 条数据，100 条/页）
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task PaginatedQuery_10000Questions_100PerPage_ShouldCompleteWithinThreshold()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateNewFormatQuestions(10000);
        
        {
            await using var setupCtx = CreateSqliteContext();
            await setupCtx.Database.EnsureCreatedAsync();
            setupCtx.Questions.AddRange(questions);
            await setupCtx.SaveChangesAsync();
        }
        
        await using var queryCtx = CreateSqliteContext();
        
        // Act - 模拟分页查询
        var totalPages = 100;
        var sw = Stopwatch.StartNew();
        
        for (int page = 0; page < totalPages; page++)
        {
            var pagedResult = await queryCtx.Questions
                .OrderBy(q => q.Id)
                .Skip(page * 100)
                .Take(100)
                .ToListAsync();
        }
        
        sw.Stop();
        
        // Assert
        _output.WriteLine($"分页查询 10000 条数据（100 页）总耗时: {sw.ElapsedMilliseconds}ms");
        // 平均每页查询应在 100ms 以内
        sw.ElapsedMilliseconds.Should().BeLessThan(10000, "分页查询 10000 条数据应在 10 秒内完成");
    }

    #endregion

    #region 一致性检查性能测试

    /// <summary>
    /// 1000 条数据的一致性检查应在 2 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task ConsistencyCheck_1000Questions_ShouldCompleteWithin2Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateMixedFormatQuestions(1000, 0.5);
        
        {
            await using var setupCtx = CreateSqliteContext();
            await setupCtx.Database.EnsureCreatedAsync();
            setupCtx.Questions.AddRange(questions);
            await setupCtx.SaveChangesAsync();
        }
        
        // Act
        var sw = Stopwatch.StartNew();
        var report = await CheckConsistencyAsync();
        sw.Stop();
        
        // Assert
        _output.WriteLine($"1000 条数据一致性检查耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(2000, "1000 条数据的一致性检查应在 2 秒内完成");
    }

    /// <summary>
    /// 5000 条数据的一致性检查应在 10 秒内完成
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task ConsistencyCheck_5000Questions_ShouldCompleteWithin10Seconds()
    {
        // Arrange
        var questions = TestDataGenerator.GenerateMixedFormatQuestions(5000, 0.5);
        
        {
            await using var setupCtx = CreateSqliteContext();
            await setupCtx.Database.EnsureCreatedAsync();
            setupCtx.Questions.AddRange(questions);
            await setupCtx.SaveChangesAsync();
        }
        
        // Act
        var sw = Stopwatch.StartNew();
        var report = await CheckConsistencyAsync();
        sw.Stop();
        
        // Assert
        _output.WriteLine($"5000 条数据一致性检查耗时: {sw.ElapsedMilliseconds}ms");
        sw.ElapsedMilliseconds.Should().BeLessThan(10000, "5000 条数据的一致性检查应在 10 秒内完成");
    }

    #endregion

    #region 性能基准测试

    /// <summary>
    /// 建立性能基线并记录测试结果
    /// </summary>
    [Fact(Skip = "性能测试，手动运行")]
    public async Task PerformanceBaseline_ShouldEstablishBaselineMetrics()
    {
        // Arrange
        var baselineResults = new List<PerformanceResult>();
        var batchSizes = new[] { 50, 100, 200, 500 };
        var questionCounts = new[] { 1000, 5000 };
        
        foreach (var questionCount in questionCounts)
        {
            foreach (var batchSize in batchSizes)
            {
                var questions = TestDataGenerator.GenerateOldFormatQuestions(questionCount);
                
                await using var context = CreateSqliteContext();
                await context.Database.EnsureCreatedAsync();
                
                // Act
                var sw = Stopwatch.StartNew();
                await MigrateQuestionsInBatchesAsync(context, questions, batchSize);
                sw.Stop();
                
                baselineResults.Add(new PerformanceResult
                {
                    QuestionCount = questionCount,
                    BatchSize = batchSize,
                    ElapsedMilliseconds = sw.ElapsedMilliseconds
                });
            }
        }
        
        // Assert - 输出性能基准结果
        _output.WriteLine("=== 性能基准测试结果 ===");
        foreach (var result in baselineResults.OrderBy(r => r.QuestionCount).ThenBy(r => r.BatchSize))
        {
            _output.WriteLine($"题目数量: {result.QuestionCount}, 批次大小: {result.BatchSize}, 耗时: {result.ElapsedMilliseconds}ms, " +
                            $"平均每条: {result.ElapsedMilliseconds / (double)result.QuestionCount:F2}ms");
        }
    }

    #endregion

    #region 辅助方法

    private static AnswerMeDbContext CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddDbContextFactory<AnswerMeDbContext>(options =>
        {
            options.UseSqlite(connection);
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IDbContextFactory<AnswerMeDbContext>>();
        var context = factory.CreateDbContext();

        return context;
    }

    private static async Task MigrateQuestionsInBatchesAsync(
        AnswerMeDbContext context,
        List<Question> questions,
        int batchSize)
    {
        for (int i = 0; i < questions.Count; i += batchSize)
        {
            var batch = questions.Skip(i).Take(batchSize).ToList();
            
            foreach (var question in batch)
            {
                MigrateOldToNew(question);
            }
            
            context.Questions.AddRange(batch);
            await context.SaveChangesAsync();
            
            // 清除变更追踪以释放内存
            context.ChangeTracker.Clear();
        }
    }

    private static Question MigrateOldToNew(Question question)
    {
        var type = question.QuestionType?.ToLowerInvariant() ?? "";
        
        question.QuestionTypeEnum = type switch
        {
            "choice" or "single" => QuestionType.SingleChoice,
            "multiple" => QuestionType.MultipleChoice,
            "true-false" or "boolean" => QuestionType.TrueFalse,
            "fill" or "fillblank" => QuestionType.FillBlank,
            "essay" or "shortanswer" or "short" => QuestionType.ShortAnswer,
            _ => QuestionType.SingleChoice
        };
        
        question.QuestionDataJson = type switch
        {
            "choice" or "single" or "multiple" => JsonSerializer.Serialize(new
            {
                type = "choice",
                options = ParseOptions(question.Options),
                correctAnswers = ParseCorrectAnswers(question.CorrectAnswer),
                explanation = question.Explanation,
                difficulty = question.Difficulty
            }),
            "true-false" or "boolean" => JsonSerializer.Serialize(new
            {
                type = "boolean",
                correctAnswer = ParseBooleanAnswer(question.CorrectAnswer),
                explanation = question.Explanation,
                difficulty = question.Difficulty
            }),
            "fill" or "fillblank" => JsonSerializer.Serialize(new
            {
                type = "fillBlank",
                correctAnswer = question.CorrectAnswer,
                explanation = question.Explanation,
                difficulty = question.Difficulty
            }),
            _ => JsonSerializer.Serialize(new
            {
                type = "shortAnswer",
                correctAnswer = question.CorrectAnswer,
                explanation = question.Explanation,
                difficulty = question.Difficulty
            })
        };
        
        return question;
    }

    private static List<string> ParseOptions(string? options)
    {
        if (string.IsNullOrWhiteSpace(options)) return new List<string>();
        
        if (options.StartsWith("["))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(options) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        
        return options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static List<string> ParseCorrectAnswers(string? correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(correctAnswer)) return new List<string>();
        
        if (correctAnswer.TrimStart().StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(correctAnswer) ?? new List<string>();
            }
            catch
            {
                // 继续尝试其他方式
            }
        }
        
        return correctAnswer
            .Split(new[] { ',', ';', '、' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToList();
    }

    private static bool? ParseBooleanAnswer(string? correctAnswer)
    {
        if (string.IsNullOrWhiteSpace(correctAnswer)) return null;
        
        var normalized = correctAnswer.Trim().ToLowerInvariant();
        var trueValues = new[] { "true", "t", "1", "yes", "y", "on", "对", "正确", "是" };
        var falseValues = new[] { "false", "f", "0", "no", "n", "off", "错", "错误", "否" };
        
        if (trueValues.Contains(normalized)) return true;
        if (falseValues.Contains(normalized)) return false;
        return null;
    }

    private static async Task<DataConsistencyReport> CheckConsistencyAsync()
    {
        await using var context = CreateSqliteContext();
        
        var questions = await context.Questions
            .Select(q => new
            {
                q.Id,
                q.QuestionType,
                q.QuestionDataJson,
                q.Options,
                q.CorrectAnswer
            })
            .ToListAsync();
        
        var report = new DataConsistencyReport
        {
            TotalQuestions = questions.Count
        };
        
        foreach (var question in questions)
        {
            var issue = CheckQuestionConsistency(question);
            if (issue != null)
            {
                report.Issues.Add(issue);
            }
        }
        
        report.InconsistentQuestions = report.Issues.Count;
        report.ConsistencyRate = report.TotalQuestions > 0
            ? (double)(report.TotalQuestions - report.InconsistentQuestions) / report.TotalQuestions * 100
            : 100;
        
        return report;
    }

    private static DataConsistencyIssue? CheckQuestionConsistency(dynamic question)
    {
        var questionType = question.QuestionType as string;
        var questionDataJson = question.QuestionDataJson as string;
        
        if (string.IsNullOrWhiteSpace(questionDataJson))
        {
            var hasLegacyData = !string.IsNullOrWhiteSpace(question.Options)
                || !string.IsNullOrWhiteSpace(question.CorrectAnswer);
            
            if (hasLegacyData)
            {
                return new DataConsistencyIssue
                {
                    QuestionId = question.Id,
                    IssueType = "MissingQuestionDataJson",
                    Description = "QuestionDataJson 为空，但存在旧字段数据",
                    Severity = "Warning"
                };
            }
            
            return new DataConsistencyIssue
            {
                QuestionId = question.Id,
                IssueType = "NoDataAtAll",
                Description = "题目没有任何数据",
                Severity = "Error"
            };
        }
        
        return null;
    }

    #endregion
}

/// <summary>
/// 性能测试结果
/// </summary>
public class PerformanceResult
{
    public int QuestionCount { get; set; }
    public int BatchSize { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

/// <summary>
/// 数据一致性报告（简化版）
/// </summary>
public class DataConsistencyReport
{
    public int TotalQuestions { get; set; }
    public int InconsistentQuestions { get; set; }
    public double ConsistencyRate { get; set; }
    public List<DataConsistencyIssue> Issues { get; set; } = new();
}

/// <summary>
/// 数据一致性问题
/// </summary>
public class DataConsistencyIssue
{
    public int QuestionId { get; set; }
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
}
