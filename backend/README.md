# AnswerMe Backend 项目结构

## 技术栈
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- SQLite (开发) / PostgreSQL (生产)
- Redis (AI 异步任务队列)
- JWT认证

## Clean Architecture 分层

```
backend/
├── AnswerMe.Domain/           # 领域层(核心业务逻辑)
│   ├── Entities/              # 实体(User, QuestionBank, Question等)
│   ├── ValueObjects/          # 值对象
│   ├── Interfaces/            # 接口定义(仓储、领域服务等)
│   └── Exceptions/            # 领域异常
│
├── AnswerMe.Application/      # 应用层(业务用例)
│   ├── Interfaces/            # 接口(如IAIProvider)
│   ├── Services/              # 应用服务(QuestionService, AuthService等)
│   ├── DTOs/                  # 数据传输对象
│   └── Validators/            # FluentValidation验证器
│
├── AnswerMe.Infrastructure/   # 基础设施层(外部依赖)
│   ├── Data/                  # EF Core(DbContext, Migrations)
│   └── Repositories/          # 仓储实现
│
└── AnswerMe.API/              # 表现层(API端点)
    ├── Controllers/           # API控制器
    ├── Filters/               # 全局过滤器(异常、验证等)
    └── Middleware/            # 中间件(JWT、CORS等)
```

## 依赖关系规则(Clean Architecture)

```
API → Application → Domain
      ↓
   Infrastructure → Domain
```

**重要原则**:
- ✅ Domain层不依赖任何其他层
- ✅ Application层只依赖Domain层
- ✅ Infrastructure层实现Domain层定义的仓储接口
- ✅ API层协调Application和Infrastructure层

## 项目启动

### 开发环境

#### 方式一：最小化启动（推荐快速开发）

只启动 API，使用 SQLite 数据库（无需额外依赖）：

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project AnswerMe.API
```

> 注意：此模式下 AI 异步生成功能不可用，但同步生成（≤20 题目）正常工作。

#### 方式二：完整启动（包含 Redis）

启动 PostgreSQL + Redis，启用所有功能：

```bash
# 1. 启动依赖服务
docker-compose up -d db redis

# 2. 配置环境变量（如需要）
# 编辑 AnswerMe.API/appsettings.json
# 设置 DB_TYPE=PostgreSQL
# 设置 ConnectionStrings:Redis=localhost:6379

# 3. 启动 API
cd backend
dotnet run --project AnswerMe.API
```

### 生产构建
```bash
dotnet publish -c Release -o ./publish
```

## 数据库迁移
```bash
# 创建迁移
dotnet ef migrations add InitialCreate --project AnswerMe.Infrastructure

# 应用迁移
dotnet ef database update --project AnswerMe.Infrastructure
```

## 环境变量配置
参见 `.env.example` 文件配置:

### 必需配置
```bash
# JWT 密钥（至少32字符）
JWT_SECRET=your-secret-key-minimum-32-characters-long
```

### 可选配置
```bash
# 数据库类型（默认: Sqlite）
DB_TYPE=Sqlite

# Redis 连接（用于 AI 异步生成）
ConnectionStrings__Redis=localhost:6379

# AI 生成配置
AIGeneration__MaxSyncCount=20          # 同步生成最大题目数
AIGeneration__WorkerConcurrency=1      # 后台工作线程数
```

### Redis 说明

Redis 用于 AI 异步生成任务队列：
- **题目数量 ≤ MaxSyncCount**：同步生成，直接返回结果
- **题目数量 > MaxSyncCount**：异步生成，任务进入 Redis 队列

如果不需要异步生成功能，可以不配置 Redis。

## API端点
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/questionbanks` - 获取题库列表
- `POST /api/questions/generate` - AI生成题目
- `GET /health` - 健康检查

详细API文档参见仓库 `docs/api.md` 与控制器源码
