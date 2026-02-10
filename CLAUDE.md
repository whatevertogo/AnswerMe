# CLAUDE.md

## 项目梗概

**AnswerMe** - 自托管智能题库系统，支持 AI 生成题目、多题库管理、练习/测验模式。

### 后端架构 (backend/)
```
AnswerMe.Domain/          # 实体、仓储接口（无依赖）
AnswerMe.Application/     # 业务逻辑、DTOs、服务接口
AnswerMe.Infrastructure/  # EF Core、仓储实现、AI Providers
AnswerMe.API/             # 控制器、Program.cs、BackgroundServices
```
依赖流: Domain ← Application ← Infrastructure ← API

### 前端架构 (frontend/src/)
```
api/           # API 请求函数（按模块划分）
stores/        # Pinia 状态管理
types/         # TypeScript 类型定义
views/         # 页面组件
components/    # 可复用组件
composables/   # 组合式函数
router/        # 路由配置
utils/         # 工具函数（request.ts 统一 HTTP）
```

## 快速启动

```bash
# 后端
cd backend && dotnet run --project AnswerMe.API

# 前端
cd frontend && npm run dev

# 完整服务（含 Redis）
docker-compose up -d
```

## Question 数据模型（迁移中）

**已废弃:** `Options`, `CorrectAnswer`
**当前:** `QuestionTypeEnum`, `QuestionDataJson`, `Data` (运行时)

**映射规则:**
- Entity → DTO: 只映射 `QuestionTypeEnum`, `Data`
- DTO → Entity: 只写入 `QuestionDataJson`

## 关键文件

- `Program.cs` - DI、中间件、Serilog
- `Domain/Common/LegacyFieldParser.cs` - 旧字段解析统一入口
- `Infrastructure/Data/AnswerMeDbContext.cs` - DbContext

## 环境变量

**必需:** `JWT_SECRET` (≥32字符)
**可选:** `DB_TYPE` (Sqlite/PostgreSQL), `ConnectionStrings__Redis`

## 前端规范

- 常量: `types/question.ts`
- API: `api/xxx.ts`，使用 **命名导入** `import { request }`
- Element Plus v3+: `el-radio`/`el-checkbox` 用 `value` 非 `label`

## 后端规范

- 仓储计数: `CountByXxxYyyAsync`
- 统计功能: DTO → Interface → Service → Controller
- Application 层不直接引用 EFCore

## 常见坑点

- 迁移前停止 API（SQLite 锁定）
- EF 工具失效: `dotnet tool install --global dotnet-ef`
- AI 异步生成需 Redis（`docker-compose up -d redis`）
