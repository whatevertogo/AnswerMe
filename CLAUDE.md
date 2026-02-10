# CLAUDE.md

## 项目架构

**AnswerMe** - 自托管智能题库系统

**技术栈:** .NET 10 + Vue 3 + SQLite/PostgreSQL

**Clean Architecture 4层:**
- `Domain/` - 实体、仓储接口
- `Application/` - 业务逻辑、DTOs、服务
- `Infrastructure/` - EF Core、仓储实现、AI Providers
- `API/` - 控制器、启动配置

**关键规则:** 依赖单向流动 → Domain ← Application ← Infrastructure ← API

## 核心命令

```bash
# 后端
cd backend && dotnet run --project AnswerMe.API
dotnet ef migrations add Name --project Infrastructure --startup-project API
dotnet test --filter "ClassName~Xxx"

# 前端
cd frontend && npm run dev
npm run test -- xxx.test.ts

# Docker
docker-compose up -d
```

## 关键架构模式

### Question 数据模型（迁移中）

**旧字段（已废弃）:** `Options`, `CorrectAnswer`

**新字段（当前）:** `QuestionTypeEnum`, `QuestionDataJson`, `Data` (运行时属性)

**Entity → DTO:** 只映射 `QuestionTypeEnum`, `Data`
**DTO → Entity:** 只写入 `QuestionDataJson`，不更新旧字段

**Data 层次:**
```
QuestionData
├── ChoiceQuestionData (Options[], CorrectAnswers[])
├── BooleanQuestionData (CorrectAnswer: bool)
├── FillBlankQuestionData (AcceptableAnswers[])
└── ShortAnswerQuestionData (ReferenceAnswer)
```

### 仓储模式

```csharp
// Domain/Interfaces/ - 接口
// Infrastructure/Repositories/ - 实现
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
```

### AI Provider 工厂

```csharp
// Infrastructure/AI/ - IAIProvider 实现
// 注册为 SINGLETON
builder.Services.AddSingleton<IAIProvider, OpenAIProvider>();
var provider = _factory.GetProvider("OpenAI");
```

### 乐观锁

```csharp
// QuestionBank.Version: byte[8]
if (!dto.Version.SequenceEqual(entity.Version))
    throw new InvalidOperationException("并发冲突");
BitConverter.GetBytes(BitConverter.ToInt64(entity.Version) + 1)
    .CopyTo(entity.Version, 0);
```

### 基于游标分页

```csharp
query.Take(pageSize + 1)  // 多取1条判断是否有更多
if (lastId.HasValue) query = query.Where(x => x.Id < lastId);
```

### JWT 认证

```csharp
// 控制器中提取用户ID
int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
```

## 关键文件

- `Program.cs` - DI 注册、中间件、Serilog 配置
- `Infrastructure/Data/AnswerMeDbContext.cs` - DbContext
- `Domain/Entities/Question.cs` - 注意 Data 属性的自动回退逻辑
- `Application/Common/EntityMappingExtensions.cs` - 实体→DTO 映射

## 环境变量

**必需:** `JWT_SECRET` (≥32字符)

**重要:** `DB_TYPE` (Sqlite/PostgreSQL), `ALLOWED_ORIGINS`

## 常见问题

- **EF工具失效:** `dotnet tool install --global dotnet-ef`
- **SQLite锁定:** 停止API后再运行迁移
- **Serilog静态方法:** 用 `Log.Information()` 不是 `LogILogger()`
- **Program.cs中日志:** `Serilog.Events.LogEventLevel` 不是 `Microsoft.Extensions.Logging.LogLevel`

## 添加新实体流程

1. `Domain/Entities/` 创建实体
2. `Domain/Interfaces/` 仓储接口
3. `Infrastructure/Repositories/` 仓储实现
4. `Application/Interfaces/` 服务接口
5. `Application/Services/` 服务实现
6. `API/Controllers/` 控制器
7. `Program.cs` 注册所有服务
8. `dotnet ef migrations add`
