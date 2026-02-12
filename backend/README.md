# AnswerMe Backend

自托管智能题库系统后端 API。

## 技术栈

- .NET 8, EF Core, SQLite/PostgreSQL, Redis

## 架构

```
AnswerMe.Domain/          # 实体、仓储接口
AnswerMe.Application/     # 业务逻辑、DTOs、服务接口
AnswerMe.Infrastructure/  # EF Core、仓储实现、AI Providers
AnswerMe.API/             # 控制器、BackgroundServices
```

依赖流: Domain ← Application ← Infrastructure ← API

## 启动

```bash
# 完整启动（包含 Redis）
docker-compose up -d redis
cd backend && dotnet run --project AnswerMe.API

# 最小化启动（SQLite，无需 Redis）
cd backend && dotnet run --project AnswerMe.API
```

## 环境变量

| 变量 | 必需 | 说明 |
|-----|------|-----|
| JWT_SECRET | 是 | ≥32字符 |
| DB_TYPE | 否 | Sqlite/PostgreSQL |
| ConnectionStrings__DefaultConnection | 否 | 数据库连接 |
| ConnectionStrings__Redis | 否 | Redis 连接 |

## API 端点

| 模块 | 端点 | 说明 |
|-----|------|-----|
| Auth | POST /api/auth/login | 登录 |
| Auth | POST /api/auth/register | 注册 |
| QuestionBanks | GET /api/questionbanks | 题库列表 |
| QuestionBanks | POST /api/questionbanks | 创建题库 |
| Questions | GET /api/questions | 搜索题目 |
| Questions | POST /api/questions | 创建题目 |
| Questions | POST /api/questions/generate | AI 生成 |
| Attempts | POST /api/attempts | 开始练习/测验 |
| Attempts | POST /api/attempts/{id}/submit | 提交答案 |
| Stats | GET /api/stats/overview | 统计概览 |
| Health | GET /health | 健康检查 |

## AI 提供商

OpenAI、Anthropic、DeepSeek、Qwen、Zhipu、Minimax、Custom API

## 代码规范

- 仓储计数: `CountByXxxYyyAsync`
- 统计功能: DTO → Interface → Service → Controller
- Application 层不引用 EFCore

## 数据模型

Question 使用 `QuestionTypeEnum` + `QuestionDataJson`：
- `SingleChoice` → `choice`
- `TrueFalse` → `boolean`
- `FillBlank` → `fillBlank`
- `ShortAnswer` → `shortAnswer`

## 关键文件

- `Program.cs` - DI、中间件、Serilog
- `Domain/Common/LegacyFieldParser.cs` - 旧字段解析
- `Infrastructure/Data/AnswerMeDbContext.cs` - DbContext
- `Infrastructure/Services/DataConsistencyCheckService.cs` - 数据一致性检查
