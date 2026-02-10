# 后端代码审查问题清单

> 审查日期: 2026-02-10
> 审查范围: Domain + Application + Infrastructure + API 层

---

## 高优先级问题

### 1. AIGenerationService - 静态字典存储异步任务状态
**文件**: `AnswerMe.Application/Services/AIGenerationService.cs:30-31`

**问题描述**:
```csharp
// 内存中的异步任务存储（生产环境应使用Redis或数据库）
private static readonly Dictionary<string, AIGenerateProgressDto> _asyncTasks = new();
private static readonly object _taskLock = new();
```
- 应用重启后所有任务状态丢失
- 多实例部署时状态无法共享
- 内存无上限，长期运行可能OOM

**修复建议**:
实现 IDistributedCache (Redis) 或数据库持久化：
```csharp
private readonly IDistributedCache _cache;

public async Task<string> StartAsyncGeneration(...)
{
    var taskId = Guid.NewGuid().ToString();
    var progress = new AIGenerateProgressDto { ... };
    await _cache.SetAsync(taskId, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(progress)));
    return taskId;
}
```

---

### 2. AIGenerationService - Fire-and-forget DbContext 风险
**文件**: `AnswerMe.Application/Services/AIGenerationService.cs:95-96`

**问题描述**:
```csharp
_ = Task.Run(async () => await ExecuteAsyncGeneration(taskId, userId, dto), cancellationToken)
    .ConfigureAwait(false);
```
Fire-and-forget 的后台任务中使用注入的依赖，原始请求完成后 DbContext 可能被释放。

**修复建议**:
使用 `IHostedService` 或 `BackgroundService` 处理后台任务，或使用消息队列。

---

### 3. AttemptService.GetStatisticsAsync - N+1 查询性能问题
**文件**: `AnswerMe.Application/Services/AttemptService.cs:284-298`

**问题描述**:
```csharp
for (int i = 0; i < attemptIds.Count; i += batchSize)
{
    var batchIds = attemptIds.Skip(i).Take(batchSize).ToList();
    foreach (var attemptId in batchIds)
    {
        var attemptDetails = await _attemptDetailRepository.GetByAttemptIdAsync(attemptId, cancellationToken);
        allDetails.AddRange(attemptDetails);
    }
}
```
循环中逐个查询 AttemptDetail，即使分批仍然是 N+1 问题。

**修复建议**:
添加批量查询方法：
```csharp
// IAttemptDetailRepository.cs
Task<List<AttemptDetail>> GetByAttemptIdsAsync(List<int> attemptIds, CancellationToken cancellationToken);

// AttemptService.cs
var allDetails = await _attemptDetailRepository.GetByAttemptIdsAsync(attemptIds, cancellationToken);
```

---

### 4. EntityMappingExtensions.ToDtoListAsync - 效率低下
**文件**: `AnswerMe.Application/Common/EntityMappingExtensions.cs:98-111`

**问题描述**:
```csharp
foreach (var entity in entities)
{
    result.Add(await entity.ToDtoAsync(questionRepository, cancellationToken));
}
```
- 串行等待每个异步调用
- N+1 问题：每个题库都单独查询题目数量

**修复建议**:
```csharp
// 批量获取所有题库的题目数量
var bankIds = entities.Select(e => e.Id).ToList();
var counts = await questionRepository.CountByQuestionBankIdsAsync(bankIds, cancellationToken);

// 并行处理
var tasks = entities.Select(e => e.ToDtoAsync(...));
return (await Task.WhenAll(tasks)).ToList();
```

---

### 5. QuestionService.GetListAsync - 过滤逻辑在内存中执行
**文件**: `AnswerMe.Application/Services/QuestionService.cs:104-109`

**问题描述**:
```csharp
var filteredQuestions = questionsPaged
    .Where(q =>
        (string.IsNullOrEmpty(query.Difficulty) || q.Difficulty == query.Difficulty) &&
        (string.IsNullOrEmpty(typeFilterPaged) || q.QuestionType == typeFilterPaged))
    .ToList();
```
过滤在获取数据后进行，可能导致返回数量少于 PageSize。

**修复建议**:
将过滤条件下推到 Repository 层，或在文档中说明这是"后过滤"行为。

---

## 中优先级问题

### 6. Question.Data 属性 - 复杂的回退逻辑
**文件**: `AnswerMe.Domain/Entities/Question.cs:46-67`

**问题描述**:
```csharp
[NotMapped]
public QuestionData? Data
{
    get
    {
        if (string.IsNullOrWhiteSpace(QuestionDataJson))
            return BuildDataFromLegacy();
        // 每次都反序列化，无缓存
    }
}
```
- 每次访问都触发 JSON 反序列化
- 回退逻辑在每次访问时都执行
- 没有缓存机制

**修复建议**:
添加私有缓存字段或明确区分新/旧数据。

---

### 7. DataSource.Config 字段安全隐患
**文件**: `AnswerMe.Domain/Entities/DataSource.cs:11`

**问题描述**:
`Config` 字段存储 JSON 配置，可能包含 API 密钥等敏感信息，缺乏加密/解密机制的设计。

**修复建议**:
在 Infrastructure 层实现配置字段的自动加密/解密。

---

### 8. 仓储接口返回类型不一致
**文件**: `AnswerMe.Domain/Interfaces/IUserRepository.cs`

**问题描述**:
`AddAsync` 和 `UpdateAsync` 返回实体，违反了仓储模式的无返回设计原则。

**修复建议**:
```csharp
Task<int> AddAsync(User user, CancellationToken cancellationToken = default);
// 或
Task AddAsync(User user, CancellationToken cancellationToken = default);
```

---

### 9. AttemptDetail.IsCorrect 可空性不一致
**文件**: `AnswerMe.Domain/Entities/AttemptDetail.cs:11`

**问题描述**:
`bool? IsCorrect` 为可空，但没有明确的业务语义。对于简答题等主观题型，`null` 表示"未批改"还是"无法判断"不清晰。

**修复建议**:
```csharp
public enum GradingStatus
{
    NotGraded, Correct, Incorrect, Partial
}
public GradingStatus GradingStatus { get; set; }
```

---

### 10. 缺少软删除支持
**文件**: `AnswerMe.Domain/Entities/BaseEntity.cs`

**问题描述**:
`BaseEntity` 缺少软删除标记字段（如 `IsDeleted`），数据删除后无法恢复。

**修复建议**:
```csharp
public bool IsDeleted { get; set; } = false;
public DateTime? DeletedAt { get; set; }
```

---

### 11. Email/Username 缺少唯一约束
**文件**: `AnswerMe.Domain/Entities/User.cs:9-10`

**问题描述**:
`Email` 和 `Username` 字段未标注唯一性约束，仓储层虽然有 `ExistsAsync` 检查，但数据库层面无约束保证数据一致性。

**修复建议**:
在 Infrastructure 层的 FluentAPI 配置中添加唯一索引。

---

### 12. QuestionType 字符串字段冗余
**文件**: `AnswerMe.Domain/Entities/Question.cs:22`

**问题描述**:
`QuestionType` 字符串字段 + `QuestionTypeEnum` 枚举属性存在冗余，虽然是为了兼容旧格式，但增加了维护复杂度。

**修复建议**:
明确迁移计划：何时移除 `QuestionType` 字符串字段，添加 `[Obsolete]` 标记。

---

## 低优先级问题

### 13. 魔术数字和字符串
**位置**: 多处

**问题描述**:
- `AIGenerationService.cs:32` - `GenerationBatchSize = 5`
- `AIGenerationService.cs:57` - `dto.Count > 20`

**修复建议**: 提取为配置常量或 appsettings。

---

### 14. 缺少审计字段
**文件**: `AnswerMe.Domain/Entities/BaseEntity.cs`

**问题描述**:
缺少 `CreatedBy`、`UpdatedBy` 审计字段，无法追踪操作用户。

**修复建议**:
```csharp
public int? CreatedBy { get; set; }
public int? UpdatedBy { get; set; }
```

---

### 15. OrderIndex 缺少范围验证
**文件**: `AnswerMe.Domain/Entities/Question.cs:127`

**问题描述**:
`OrderIndex` 为 `int`，没有最小值约束，负数可能引起排序问题。

**修复建议**:
添加 `uint` 或在业务层验证非负。

---

### 16. QuestionDataJsonOptions 缺少配置灵活性
**文件**: `AnswerMe.Domain/Models/QuestionData.cs:32-38`

**问题描述**:
`QuestionDataJsonOptions.Default` 写死为 camelCase + 不缩进，不支持自定义配置。

**修复建议**:
如果需要调试，可以从配置读取。

---

### 17. Score 字段类型不明确
**文件**: `AnswerMe.Domain/Entities/Attempt.cs:12`

**问题描述**:
`decimal? Score` 类型不够精确，无法区分百分比分数（0-100）和比率分数（0-1）。

**修复建议**:
明确分数表示方式，或添加 `MaxScore` 字段。

---

## 设计建议

### 1. 实现值对象模式
考虑将 `Difficulty`、`QuestionType` 等从字符串/枚举改为值对象。

### 2. 明确聚合根边界
`QuestionBank` 应该是聚合根，管理 `Question` 的生命周期。

### 3. 添加领域事件支持
```csharp
public abstract class BaseEntity
{
    private List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
}
```

### 4. 添加数据库索引
- `QuestionBank.UserId`
- `Question.QuestionBankId`
- `Attempt.UserId`
- `User.Email`, `User.Username`

### 5. 考虑添加 CQRS 模式
分离查询和命令，提高性能。

---

## 统计

| 优先级 | Domain | Application | Infrastructure | API | 合计 |
|--------|--------|-------------|----------------|-----|------|
| 高 | 3 | 2 | 0 | 0 | 5 |
| 中 | 5 | 1 | 1 | 0 | 7 |
| 低 | 3 | 1 | 0 | 0 | 4 |
| **合计** | **11** | **4** | **1** | **0** | **16** |
