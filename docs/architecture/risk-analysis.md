# AnswerMe 风险分析与缓解措施

## 目录
1. [技术风险](#技术风险)
2. [安全风险](#安全风险)
3. [产品风险](#产品风险)
4. [运维风险](#运维风险)
5. [合规风险](#合规风险)
6. [风险监控指标](#风险监控指标)

---

## 技术风险

### 1. AI调用不稳定

**风险描述**：
- 用户配置的AI API可能遇到限流（Rate Limit）
- 网络超时导致请求失败
- API服务不可用（ downtime）
- 返回格式错误或非预期内容

**影响等级**：🔴 高
**发生概率**：高

**缓解措施**：

1. **重试机制**
```csharp
// 使用Polly实现指数退避重试
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TaskCanceledException>()
    .Or<AIProviderException>(ex => ex.IsTransient)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, delay, retryCount, context) =>
        {
            _logger.LogWarning(
                "Retry {RetryCount} after {Delay}s due to: {ErrorMessage}",
                retryCount, delay.TotalSeconds, outcome.Exception?.Message);
        });
```

2. **超时控制**
```csharp
var httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(30); // 30秒超时
```

3. **降级策略**
- 重试失败后返回友好错误提示
- 提供手动重试按钮
- 保存生成失败的部分题目

4. **监控告警**
- 记录失败率
- 连续失败超过阈值时告警

**验证方法**：
- 单元测试：模拟超时、限流场景
- 集成测试：使用mock API测试重试逻辑

---

### 2. 自定义API配置复杂

**风险描述**：
- 用户配置错误导致无法成功生成题目
- JSONPath表达式错误无法解析响应
- 请求格式不符合API要求

**影响等级**：🟡 中
**发生概率**：中

**缓解措施**：

1. **配置验证**
```csharp
public async Task<ValidationResult> ValidateConfigAsync(CustomApiConfig config)
{
    var errors = new List<string>();

    // 1. URL格式验证
    if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out var uri))
    {
        errors.Add("Invalid API endpoint URL");
    }

    // 2. JSONPath语法验证
    try
    {
        var dummyJson = "{\"data\":{\"questions\":[{\"text\":\"test\"}]}";
        JToken.Parse(dummyJson).SelectToken(config.ResponseMapping.QuestionsPath);
    }
    catch (Exception ex)
    {
        errors.Add($"Invalid JSONPath: {ex.Message}");
    }

    // 3. 实际API调用测试
    try
    {
        var testRequest = new QuestionGenerationRequest { Prompt = "test", Count = 1 };
        var provider = new CustomApiProvider(_httpClient, config);
        var result = await provider.GenerateQuestionsAsync(testRequest);

        if (!result.Success)
        {
            errors.Add($"API test failed: {result.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        errors.Add($"API test error: {ex.Message}");
    }

    return errors.Count == 0
        ? ValidationResult.Success()
        : ValidationResult.Failure(errors);
}
```

2. **预设模板库**
提供常见API的配置模板（OpenAI兼容、简单REST API等）

3. **测试按钮**
在保存配置前提供测试按钮，实时反馈配置是否有效

4. **详细错误信息**
明确指出配置错误位置和修复建议

5. **示例文档**
提供配置示例和常见问题解答

---

### 3. 数据库性能瓶颈

**风险描述**：
- 题目和答题记录量大导致查询变慢
- 复杂统计查询耗时长
- 未优化索引导致全表扫描

**影响等级**：🟡 中
**发生概率**：中

**缓解措施**：

1. **索引优化**
```sql
-- 核心查询索引
CREATE INDEX idx_attempts_user_created ON Attempts(UserId, StartedAt DESC);
CREATE INDEX idx_attemptdetails_attempt ON AttemptDetails(AttemptId);
CREATE INDEX idx_questions_bank_order ON Questions(QuestionBankId, OrderIndex);
CREATE INDEX idx_questionbanks_user_tags ON QuestionBanks(UserId) WHERE Tags IS NOT NULL;

-- 复合索引
CREATE INDEX idx_attempts_user_bank ON Attempts(UserId, QuestionBankId);
```

2. **分页查询**
```csharp
public async Task<PagedResult<Question>> GetQuestionsAsync(
    int questionBankId,
    int pageNumber,
    int pageSize)
{
    return await _context.Questions
        .Where(q => q.QuestionBankId == questionBankId)
        .OrderBy(q => q.OrderIndex)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
}
```

3. **查询优化**
- 使用`AsNoTracking()`避免变更追踪
- 只查询需要的字段（投影查询）
- 避免N+1查询（Include预加载）

4. **缓存策略（V2）**
```csharp
// 使用Redis缓存热点数据
public async Task<QuestionBank> GetQuestionBankAsync(int id)
{
    var cacheKey = $"questionbank:{id}";

    var cached = await _cache.GetAsync<QuestionBank>(cacheKey);
    if (cached != null) return cached;

    var bank = await _context.QuestionBanks.FindAsync(id);
    if (bank != null)
    {
        await _cache.SetAsync(cacheKey, bank, TimeSpan.FromMinutes(10));
    }

    return bank;
}
```

5. **定期维护**
```sql
-- 定期VACUUM和ANALYZE
VACUUM ANALYZE Attempts;
```

**监控指标**：
- 查询响应时间（P50, P95, P99）
- 慢查询日志（超过1秒）
- 数据库连接池使用率

---

### 4. 并发答题冲突

**风险描述**：
- 多个用户同时答题同一题库可能产生冲突
- 数据更新导致覆盖问题
- 事务隔离级别不当导致脏读

**影响等级**：🟢 低
**发生概率**：低

**缓解措施**：

1. **乐观锁**
```csharp
public class Attempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int QuestionBankId { get; set; }
    public int Score { get; set; }
    public byte[] Version { get; set; } // 乐观锁版本号
}

// 更新时检查版本号
public async Task<bool> UpdateScoreAsync(int attemptId, int newScore)
{
    var attempt = await _context.Attempts.FindAsync(attemptId);

    attempt.Score = newScore;

    try
    {
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateConcurrencyException)
    {
        // 版本冲突，处理并发
        return false;
    }
}
```

2. **幂等性设计**
```csharp
// 使用唯一约束防止重复提交
public class AttemptDetail
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }

    // 确保同一答题会话中每道题只能提交一次
    // 数据库唯一索引: UNIQUE(AttemptId, QuestionId)
}
```

3. **事务隔离**
```csharp
using var transaction = await _context.Database.BeginTransactionAsync(
    IsolationLevel.ReadCommitted);

try
{
    // 业务逻辑
    await CreateAttemptAsync(userId, questionBankId);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

4. **队列处理（V2）**
对于高并发场景，使用消息队列异步处理答题提交

---

## 安全风险

### 1. API密钥泄露

**风险描述**：
- 用户配置的API密钥存储不当被窃取
- 日志中泄露密钥
- 传输过程被中间人攻击
- 前端可访问到密钥

**影响等级**：🔴 高
**发生概率**：中

**缓解措施**：

1. **数据库加密存储**
```csharp
public class EncryptionService
{
    private readonly byte[] _encryptionKey;

    public EncryptionService(IConfiguration config)
    {
        var key = config["Encryption:Key"];
        _encryptionKey = Convert.FromBase64String(key);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        var iv = aes.IV;
        var encrypted = ms.ToArray();

        // 组合IV和密文
        var result = new byte[iv.Length + encrypted.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(encrypted, 0, result, iv.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _encryptionKey;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}

// 使用加密服务
public class DataSourceService
{
    private readonly EncryptionService _encryption;

    public async Task SaveDataSourceAsync(DataSource dataSource)
    {
        // 加密敏感字段
        if (dataSource.Config.ContainsKey("apiKey"))
        {
            dataSource.Config["apiKey"] = _encryption.Encrypt(dataSource.Config["apiKey"]);
        }

        await _context.DataSources.AddAsync(dataSource);
        await _context.SaveChangesAsync();
    }
}
```

2. **日志脱敏**
```csharp
// Serilog配置，过滤敏感字段
Log.Logger = new LoggerConfiguration()
    .Filter.ByExcluding(logEvent =>
    {
        if (logEvent.Properties.TryGetValue("RequestBody", out var body))
        {
            return body.ToString().Contains("apiKey");
        }
        return false;
    })
    .WriteTo.Console(new ExpressionTemplate(
        "[{@t:HH:mm:ss} {@l:u3}] {@m}\n{@x}"))
    .CreateLogger();
```

3. **HTTPS强制**
```csharp
// 生产环境强制HTTPS
if (builder.Environment.IsProduction())
{
    builder.Services.AddHsts(options =>
    {
        options.Preload = true;
        options.IncludeSubDomains = true;
        options.MaxAge = TimeSpan.FromDays(365);
    });

    app.UseHsts();
    app.UseHttpsRedirection();
}
```

4. **前端脱敏显示**
```typescript
// 密钥只显示前4位和后4位
function maskApiKey(key: string): string {
  if (!key || key.length <= 8) return '****';
  return `${key.slice(0, 4)}...${key.slice(-4)}`;
}
```

5. **密钥轮换提醒**
- 定期提醒用户更新API密钥
- 显示密钥配置时间

---

### 2. 恶意请求刷接口

**风险描述**：
- 攻击者大量调用AI接口消耗用户额度
- 恶意爬虫抓取题库内容
- DDoS攻击导致服务不可用

**影响等级**：🟡 中
**发生概率**：中

**缓解措施**：

1. **Rate Limiting**
```csharp
// 使用AspNetCoreRateLimit
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("GenerateQuestions", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString(),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10, // 每10分钟最多10次
                Window = TimeSpan.FromMinutes(10),
                SegmentsPerWindow = 2
            }));
});

// 应用到端点
app.MapPost("/api/questions/generate", GenerateQuestions)
    .RequireRateLimiting("GenerateQuestions");
```

2. **请求审计日志**
```csharp
public class RequestAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestAuditMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var path = context.Request.Path;
        var method = context.Request.Method;

        // 记录敏感操作
        if (path.StartsWith("/api/questions/generate") ||
            path.StartsWith("/api/datasources"))
        {
            _logger.LogInformation(
                "User {UserId} called {Method} {Path} at {Time}",
                userId, method, path, DateTime.UtcNow);
        }

        await _next(context);
    }
}
```

3. **异常检测**
```csharp
// 检测异常行为模式
public class AnomalyDetector
{
    private const int Threshold = 100; // 每小时100次

    public async Task<bool> IsAnomalousAsync(int userId)
    {
        var count = await _context.AuditLogs
            .Where(log =>
                log.UserId == userId &&
                log.Timestamp > DateTime.UtcNow.AddHours(-1))
            .CountAsync();

        return count > Threshold;
    }
}
```

4. **验证码（V2）**
- 可疑操作要求验证码
- 使用Google reCAPTCHA

---

### 3. XSS注入

**风险描述**：
- 题目内容包含恶意脚本
- 用户输入未过滤被渲染到页面
- Cookie/Session被窃取

**影响等级**：🟡 中
**发生概率**：低

**缓解措施**：

1. **输入验证**
```csharp
public class QuestionCreateDto
{
    [Required]
    [StringLength(5000)]
    [NoHtml] // 自定义验证特性
    public string QuestionText { get; set; }
}

public class NoHtmlAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var text = value as string;
        if (string.IsNullOrEmpty(text)) return ValidationResult.Success;

        if (System.Text.RegularExpressions.Regex.IsMatch(text, "<[^>]+>"))
        {
            return new ValidationResult("HTML tags are not allowed");
        }

        return ValidationResult.Success;
    }
}
```

2. **输出转义**
```typescript
// Vue 3默认转义，避免使用v-html
<template>
  <!-- 安全：自动转义 -->
  <div>{{ question.questionText }}</div>

  <!-- 危险：不转义，谨慎使用 -->
  <div v-html="sanitizedHtml"></div>
</template>

<script setup>
import DOMPurify from 'dompurify';

function sanitizeHtml(html: string): string {
  return DOMPurify.sanitize(html);
}
</script>
```

3. **CSP策略**
```csharp
// Content Security Policy
builder.Services.AddCsp(options =>
{
    options.DefaultSources(d => d.Self());
    options.ScriptSources(d => d.Self()
        .UnsafeInline() // 如果必须使用内联脚本
        .UnsafeEval()); // 如果必须使用eval
    options.StyleSources(d => d.Self().UnsafeInline());
});
```

4. **HttpOnly Cookie**
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer()
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true; // 防止XSS窃取
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

---

### 4. SQL注入

**风险描述**：
- 拼接SQL导致注入攻击
- 用户输入未正确转义
- 数据被窃取或篡改

**影响等级**：🔴 高
**发生概率**：低

**缓解措施**：

1. **使用ORM（EF Core）**
```csharp
// ✅ 安全：参数化查询
var questions = await _context.Questions
    .Where(q => q.QuestionText.Contains(searchTerm))
    .ToListAsync();

// ❌ 危险：拼接SQL
var sql = $"SELECT * FROM Questions WHERE QuestionText LIKE '%{searchTerm}%'";
var questions = await _context.Questions.FromSqlRaw(sql).ToListAsync();
```

2. **禁止原生SQL**
```csharp
// 代码审查检查清单
// ❌ 不允许使用 FromSqlRaw / ExecuteSqlRaw
// ✅ 必须使用参数化查询
var questions = await _context.Questions
    .FromSqlRaw(
        "SELECT * FROM Questions WHERE QuestionText LIKE {0}",
        $"%{searchTerm}%")
    .ToListAsync();
```

3. **最小权限数据库用户**
```sql
-- 创建只读用户（用于查询）
CREATE USER answeruser_readonly WITH PASSWORD 'readonly_password';
GRANT SELECT ON ALL TABLES IN SCHEMA public TO answeruser_readonly;

-- 创建应用用户（CRUD）
CREATE USER answeruser WITH PASSWORD 'app_password';
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO answeruser;
-- 不授予DROP、ALTER等危险权限
```

---

## 产品风险

### 1. AI生成题目质量不可控

**风险描述**：
- AI生成的题目质量参差不齐
- 题目难度不准确
- 答案可能错误

**影响等级**：🟡 中
**发生概率**：高

**缓解措施**：

1. **Prompt工程**
```csharp
private string BuildPrompt(QuestionGenerationRequest request)
{
    return $@"
You are a professional question generator. Generate {request.Count} questions about the topic.

Requirements:
- Difficulty: {request.Difficulty}
- Question Type: {request.QuestionType}
- Output format: JSON

Output JSON structure:
{{
  ""questions"": [
    {{
      ""questionText"": ""Clear and specific question"",
      ""options"": [""A. Option1"", ""B. Option2"", ""C. Option3"", ""D. Option4""],
      ""correctAnswer"": ""A"",
      ""explanation"": ""Detailed explanation"",
      ""difficulty"": ""{request.Difficulty}""
    }}
  ]
}}

Topic: {request.Prompt}
";
}
```

2. **题目编辑功能**
- 生成后允许用户修改
- 支持重新生成单道题

3. **质量反馈机制**
```csharp
public class QuestionFeedback
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; } // 1-5星
    public string Comment { get; set; }
    public bool IsHelpful { get; set; }
}

// 根据反馈排序题目
public async Task<List<Question>> GetBestQuestionsAsync(int questionBankId)
{
    return await _context.Questions
        .Where(q => q.QuestionBankId == questionBankId)
        .OrderByDescending(q => q.Feedbacks.Average(f => f.Rating))
        .ToListAsync();
}
```

4. **人工审核模式（V2）**
- 题目生成后进入审核队列
- 管理员审核后发布

---

### 2. 依赖第三方API变更

**风险描述**：
- AI服务提供商更新API导致不可用
- API价格变更影响用户使用
- 服务停止运营

**影响等级**：🟡 中
**发生概率**：低

**缓解措施**：

1. **版本锁定**
```json
{
  "dependencies": {
    "OpenAI": "1.11.0"
  }
}
```

2. **适配器模式**
```csharp
// 通过接口隔离变化
public interface IAIProvider
{
    Task<QuestionGenerationResult> GenerateQuestionsAsync(...);
}

// 每个Provider独立实现，互不影响
public class OpenAIProvider : IAIProvider { }
public class ClaudeProvider : IAIProvider { }
```

3. **及时更新跟进**
- 订阅API变更通知
- 版本更新日志监控
- 社区反馈收集

4. **降级方案**
- 支持多个AI提供商
- 主服务不可用时切换备用

---

## 运维风险

### 1. 数据丢失

**风险描述**：
- 硬件故障导致数据丢失
- 误删除操作
- 灾难性事件

**影响等级**：🔴 高
**发生概率**：低

**缓解措施**：

1. **自动备份脚本**
```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="/backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/answermedb_${TIMESTAMP}.sql.gz"

mkdir -p ${BACKUP_DIR}

# 备份数据库
docker-compose exec -T db pg_dump -U answeruser answermedb | gzip > ${BACKUP_FILE}

# 上传到云存储（可选）
# aws s3 cp ${BACKUP_FILE} s3://my-backup-bucket/

# 清理旧备份（保留7天）
find ${BACKUP_DIR} -name "answermedb_*.sql.gz" -mtime +7 -delete

echo "Backup completed: ${BACKUP_FILE}"
```

2. **定时任务**
```bash
# crontab -e
# 每天凌晨2点备份
0 2 * * * /path/to/backup.sh >> /var/log/answerme-backup.log 2>&1
```

3. **恢复流程**
```bash
#!/bin/bash
# restore.sh

BACKUP_FILE=$1

if [ -z "$BACKUP_FILE" ]; then
    echo "Usage: ./restore.sh <backup_file>"
    exit 1
fi

echo "⚠️  WARNING: This will replace the current database!"
read -p "Are you sure? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "Aborted."
    exit 1
fi

# 停止应用
docker-compose stop backend frontend

# 恢复数据库
gunzip < ${BACKUP_FILE} | docker-compose exec -T db psql -U answeruser answermedb

# 启动应用
docker-compose start backend frontend

echo "Database restored from ${BACKUP_FILE}"
```

4. **异地备份**
- 定期备份到云存储（S3、Azure Blob）
- 多地域冗余

---

### 2. 升级失败

**风险描述**：
- 数据库迁移脚本错误
- 应用版本不兼容
- 依赖版本冲突

**影响等级**：🟡 中
**发生概率**：中

**缓解措施**：

1. **数据库迁移脚本**
```csharp
// EF Core Migrations
public class AddQuestionExplanationMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Explanation",
            table: "Questions",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Explanation",
            table: "Questions");
    }
}
```

2. **向下兼容测试**
```csharp
// 测试旧版本客户端是否能访问新版本API
[Test]
public async Task Old_Client_Should_Work_With_New_API()
{
    // Arrange
    var client = CreateOldVersionClient();
    var api = CreateNewVersionApi();

    // Act
    var response = await client.GetQuestionsAsync();

    // Assert
    Assert.IsNotNull(response);
}
```

3. **回滚方案**
```yaml
# docker-compose.yml 使用版本标签
services:
  backend:
    image: answerme/backend:1.0.0  # 具体版本标签
    # 如果升级失败，回滚到上一个版本
    # image: answerme/backend:0.9.0
```

4. **蓝绿部署（V2）**
- 运行两个版本同时在线
- 逐步切换流量
- 出问题立即回滚

---

## 合规风险

### 1. 数据隐私

**风险描述**：
- 用户个人信息泄露
- 违反GDPR等隐私法规
- 跨境数据传输

**影响等级**：🔴 高
**发生概率**：低

**缓解措施**：

1. **数据最小化**
```csharp
// 只收集必要信息
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    // 不收集手机号、地址等非必要信息
}
```

2. **数据脱敏**
```csharp
// 日志中脱敏敏感信息
public class SensitiveDataLogging
{
    public string SanitizeEmail(string email)
    {
        var parts = email.Split('@');
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
```

3. **用户同意**
- 注册时明确隐私政策
- Cookie同意提示
- 数据删除权利

---

## 风险监控指标

### 技术指标

| 指标 | 告警阈值 | 监控方式 |
|------|---------|---------|
| API错误率 | > 5% | Prometheus + Grafana |
| P95响应时间 | > 2s | APM (Application Performance Monitoring) |
| 数据库连接池使用率 | > 80% | EF Core日志 |
| 磁盘使用率 | > 85% | Docker health check |

### 业务指标

| 指标 | 正常范围 | 异常处理 |
|------|---------|---------|
| 题目生成成功率 | > 95% | 低于阈值时检查AI配置 |
| 平均答题完成率 | > 60% | 低于阈值时检查题目质量 |
| 用户活跃度 | 稳定增长 | 下降时调查原因 |

### 安全指标

| 指标 | 告警阈值 | 响应措施 |
|------|---------|---------|
| 单用户API调用频率 | > 100次/小时 | 临时封禁、人工审核 |
| 失败登录次数 | > 5次/10分钟 | 账户锁定 |
| 异常IP请求 | > 1000次/分钟 | IP黑名单 |

---

## 风险应对流程

1. **风险识别**：定期风险评估、用户反馈收集
2. **风险分析**：影响评估、概率评估
3. **风险处理**：
   - 规避：修改方案避免风险
   - 缓解：降低风险发生概率或影响
   - 转移：保险、外包
   - 接受：低风险接受并监控
4. **风险监控**：持续跟踪风险指标
5. **风险复盘**：事后分析、改进流程

---

## 总结

**最高优先级风险（需立即处理）**：
1. 🔴 API密钥泄露 → 实施加密存储
2. 🔴 SQL注入 → 使用参数化查询
3. 🔴 AI调用不稳定 → 实施重试机制

**次要优先级风险（近期处理）**：
1. 🟡 自定义API配置复杂 → 添加配置验证
2. 🟡 数据库性能 → 优化索引
3. 🟡 Rate Limiting → 防止滥用

**长期关注风险**：
1. 🟢 数据备份策略
2. 🟢 题目质量控制
3. 🟢 监控告警体系
