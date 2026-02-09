# CLAUDE.md

本文件为 Claude Code (claude.ai/code) 提供项目指导。

## 项目概述

**AnswerMe** 是一个开源的自托管智能题库系统。用户使用自己的 AI API 密钥生成题目，完全掌控学习数据。

**技术栈:**
- 后端: .NET 10 Web API + EF Core 10
- 前端: Vue 3 + TypeScript + Pinia + Element Plus
- 数据库: SQLite (开发环境) / PostgreSQL (生产环境)
- 部署: Docker Compose

**核心架构:**
Clean Architecture 四层架构:
1. **Domain** (`backend/AnswerMe.Domain/`) - 实体和接口，无依赖
2. **Application** (`backend/AnswerMe.Application/`) - 业务逻辑、DTOs、服务
3. **Infrastructure** (`backend/AnswerMe.Infrastructure/`) - EF Core、仓储、外部关注点
4. **API** (`backend/AnswerMe.API/`) - 控制器、中间件、启动配置

**关键规则:** 依赖只能向内流动。Domain → Application → Infrastructure → API。严禁反向依赖。

## 开发命令

### 后端 (.NET 10)

```bash
cd backend

# 构建解决方案
dotnet build

# 运行 API（开发环境，自动应用迁移）
dotnet run --project AnswerMe.API

# 使用 PostgreSQL 运行
set DB_TYPE=PostgreSQL
dotnet run --project AnswerMe.API

# 创建迁移
dotnet ef migrations add MigrationName --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# 手动应用迁移
dotnet ef database update --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# 运行测试
dotnet test

# 监视模式
dotnet watch --project AnswerMe.API
```

### 前端 (Vue 3 + Vite)

```bash
cd frontend

npm install          # 安装依赖
npm run dev          # 开发服务器 http://localhost:5173
npm run build        # 生产构建
npm run preview      # 预览生产构建

# 测试
npm run test               # 运行测试
npm run test:ui            # 测试 UI 界面
npm run test:coverage      # 测试覆盖率报告

# 代码质量
npm run lint               # ESLint 检查和自动修复
npm run format             # Prettier 格式化
```

### Docker

```bash
docker-compose up -d           # 启动所有服务
docker-compose logs -f         # 查看日志
docker-compose down            # 停止服务
docker-compose up -d --build   # 重新构建并启动
```

## 架构模式

### 仓储模式

所有仓储接口定义在 `Domain/Interfaces/`，实现在 `Infrastructure/Repositories/`。

```csharp
// 接口（Domain 层）
public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Question> AddAsync(Question question, CancellationToken cancellationToken = default);
}

// 注册（Program.cs）
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
```

### 服务层

服务包含业务逻辑，使用仓储（绝不直接使用 DbContext），返回 DTOs。

```csharp
// 注册
builder.Services.AddScoped<IQuestionBankService, QuestionBankService>();

// 服务模式
public class QuestionBankService : IQuestionBankService
{
    // 使用仓储，不用 DbContext
    // 返回 DTOs，不返回实体
    // 业务规则违反抛出 InvalidOperationException
}
```

### 乐观锁

`QuestionBank` 使用 `byte[] Version` 进行并发控制:

```csharp
public byte[] Version { get; set; } = new byte[8]; // 初始化为 8 字节

// 更新模式
if (dto.Version == null || !questionBank.Version.SequenceEqual(dto.Version))
{
    throw new InvalidOperationException("题库已被其他用户修改，请刷新后重试");
}
var version = BitConverter.ToInt64(questionBank.Version);
BitConverter.GetBytes(version + 1).CopyTo(questionBank.Version, 0);
```

### 基于游标的分页

```csharp
public async Task<List<QuestionBank>> GetPagedAsync(int userId, int pageSize, int? lastId)
{
    var query = _context.QuestionBanks
        .Where(qb => qb.UserId == userId)
        .OrderByDescending(qb => qb.Id)
        .Take(pageSize + 1); // 多取一条判断是否还有更多

    if (lastId.HasValue)
        query = query.Where(qb => qb.Id < lastId.Value);

    var results = await query.ToListAsync();
    return results.Take(pageSize).ToList();
}
```

### AI Provider 抽象

通过 `IAIProvider` 接口支持多个 AI 提供商:

```csharp
public interface IAIProvider
{
    string ProviderName { get; }
    Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        string apiKey, AIQuestionGenerateRequest request, CancellationToken cancellationToken);
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);
}

// 工厂通过名称检索 provider
var provider = _aiProviderFactory.GetProvider("OpenAI");
```

**添加新 provider:**
1. 在 `Application/AI/` 中实现 `IAIProvider`
2. 在 `Program.cs` 注册: `builder.Services.AddSingleton<IAIProvider, NewProvider>();`

### 数据源加密

`DataSource.Config` 使用 Data Protection API 存储加密的 API 密钥:

```csharp
// 加密
var encrypted = _dataProtector.Protect(apiKey);

// 解密
var decrypted = _dataProtector.Unprotect(encryptedConfig);

// 密钥持久化到 `keys/` 目录（90 天生命周期）
```

**安全规则:**
- 绝不记录或向前端返回 API 密钥
- 使用 API 密钥前必须解密
- 返回给客户端的 DTOs 中要掩码处理 API 密钥

### JWT 认证

```bash
# 必需的环境变量
JWT_SECRET=must-be-at-least-32-characters-long
JWT_ISSUER=AnswerMe
JWT_AUDIENCE=AnswerMeUsers
JWT_EXPIRY_DAYS=30
```

**在控制器中提取用户 ID:**
```csharp
private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var userId))
        throw new UnauthorizedAccessException("无效的用户身份");
    return userId;
}
```

### 本地认证模式

**适用场景:** 个人部署，无需注册即可使用

**配置方式:** 在 `.env` 文件中设置:
```bash
LOCAL_AUTH__ENABLE_LOCAL_LOGIN=true
LOCAL_AUTH__DEFAULT_USERNAME=LocalUser
LOCAL_AUTH__DEFAULT_EMAIL=local@answerme.local
LOCAL_AUTH__DEFAULT_PASSWORD=local123
```

启用后，可直接使用默认用户名和密码登录应用。

## 数据库

**SQLite**（默认）: API 目录下的 `answerme_dev.db` 文件。开发环境启动时自动应用迁移。

**PostgreSQL**（生产环境）: 设置环境变量 `DB_TYPE=PostgreSQL`。

**DbContext** (`Infrastructure/Data/AnswerMeDbContext.cs`):
- 自动发现 `Domain.Entities` 命名空间中的实体
- 迁移文件位于 `Infrastructure/Migrations/`
- 为关系配置了级联删除

## 前端架构

**位置:** `frontend/src/`

**Vue 3 Composition API** + TypeScript。

**Pinia stores:** `src/stores/`
- **auth** - 认证状态
- **questionBank** - 题库管理
- **question** - 题目管理
- **dataSource** - AI 数据源配置
- **aiGeneration** - AI 生成状态
- **aiConfig** - AI 配置
- **quiz** - 答题会话
- **app** - 应用全局状态

**Router:** `src/router/` - 路由守卫检查认证状态

**API 层:** `src/api/` - Axios 配置后端地址 `http://localhost:5000/api`

**CORS:** 后端默认允许 `http://localhost:5173`

## API 端点概览

主要 API 端点（后端端口默认 5000）:
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `POST /api/auth/local-login` - 本地模式登录
- `GET /api/questionbanks` - 获取题库列表
- `POST /api/questionbanks` - 创建题库
- `GET /api/questions` - 获取题目列表
- `POST /api/questions/generate` - AI 生成题目
- `POST /api/attempts` - 开始答题
- `POST /api/attempts/{id}/submit` - 提交答案
- `GET /api/datasources` - 获取数据源配置
- `POST /api/datasources/validate` - 验证 API 密钥
- `GET /health` - 健康检查

详细 API 文档请查看控制器源码: `backend/AnswerMe.API/Controllers/`

## 常用模式

### 添加新实体

1. 在 `Domain/Entities/` 创建实体
2. 在 `Domain/Interfaces/` 创建仓储接口
3. 在 `Infrastructure/Repositories/` 实现仓储
4. 在 `Application/Interfaces/` 创建服务接口
5. 在 `Application/Services/` 实现服务
6. 在 `API/Controllers/` 创建控制器
7. 在 `Program.cs` 注册所有服务
8. 创建迁移: `dotnet ef migrations add AddEntity`

### 错误处理

**全局异常过滤器**在 `Program.cs` 中注册

**服务异常:**
- `InvalidOperationException` - 业务规则违反
- `UnauthorizedAccessException` - 权限问题

**控制器模式:**
```csharp
try
{
    var result = await _service.MethodAsync(dto);
    return Ok(result);
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
catch (Exception ex)
{
    _logger.LogError(ex, "操作失败");
    return StatusCode(500, new { message = "服务器内部错误" });
}
```

### 日志

**Serilog** 在 `Program.cs` 中配置:
- 控制台输出
- 文件: `logs/answerme-.log`（保留 7 天）

**使用方式:**
```csharp
_logger.LogInformation("用户 {UserId} 执行操作", userId);
_logger.LogError(ex, "操作失败: {Data}", data);
```

## 环境变量

**必需:**
- `JWT_SECRET` - 至少 32 个字符
- `ENCRYPTION_KEY` - 生产环境用于 API 密钥加密

### 后端环境变量

**可选:**
- `DB_TYPE` - "Sqlite"（默认）或 "PostgreSQL"
- `ConnectionStrings__DefaultConnection` - 数据库连接字符串
- `JWT__Issuer`、`JWT__Audience`、`JWT__ExpiryDays` - JWT 设置
- `ALLOWED_ORIGINS` - CORS 源（默认: "http://localhost:3000,http://localhost:5173"）

### 前端环境变量

前端环境变量配置在 `frontend/.env`:

**必需:**
- `VITE_API_BASE_URL` - 后端 API 地址（默认: `http://localhost:5000`）

**可选:**
- `VITE_API_TIMEOUT` - API 请求超时时间（毫秒，默认: 10000）
- `VITE_APP_TITLE` - 应用标题
- `VITE_ENABLE_MOCK` - 启用模拟数据（默认: false）
- `VITE_ENABLE_DEBUG` - 启用调试模式（默认: true）
- `VITE_UPLOAD_MAX_SIZE` - 上传文件最大大小（字节，默认: 10485760）

### 其他可选配置

**限流配置:**
- `RATE_LIMIT_ENABLED` - 启用 API 限流（默认: false）
- `RATE_LIMIT_PER_MINUTE` - 每分钟请求限制（默认: 60）

**备份配置:**
- `BACKUP_ENABLED` - 启用自动备份（默认: false）
- `BACKUP_RETENTION_DAYS` - 备份保留天数（默认: 7）
- `BACKUP_CRON` - 备份 cron 表达式（默认: "0 2 * * *"）

**日志配置:**
- `LOG_LEVEL` - 日志级别（Debug, Information, Warning, Error，默认: Information）

## 部署

1. 复制 `.env.example` 到 `.env` 并配置
2. 设置强密码 `JWT_SECRET` 和 `ENCRYPTION_KEY`
3. 运行: `docker-compose up -d`
4. 前端: `http://localhost:3000`
5. API: `http://localhost:5000`

**健康检查:** `GET /health`

## 中文支持

项目使用中文作为面向用户的内容。所有 DTOs、实体和 API 消息支持中文文本。数据库使用 SQLite 和 PostgreSQL 正确存储中文。

项目大部分完整架构 docs/project-structure.md
