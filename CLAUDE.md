**AnswerMe** - 自托管智能题库系统，支持 AI 生成题目、多题库管理、练习/测验模式。

## 技术栈

- **后端**: .NET 8, EF Core, SQLite/PostgreSQL, Redis
- **前端**: Vue 3, TypeScript, Element Plus, Pinia, Tailwind CSS v3.x

## 架构

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
docker-compose up -d redis    # AI 异步生成依赖，必先启动
cd backend && dotnet run --project AnswerMe.API
cd frontend && npm run dev
```

## 环境变量

- **必需**: `JWT_SECRET` (≥32字符)
- **可选**: `DB_TYPE` (Sqlite/PostgreSQL), `ConnectionStrings__Redis`

## 代码规范

**后端**:
- 仓储计数: `CountByXxxYyyAsync`
- 统计功能: DTO → Interface → Service → Controller
- Application 层不直接引用 EFCore

**前端**:
- 常量: `types/question.ts`
- API: `api/xxx.ts`，使用命名导入 `import { request }`
- Element Plus v3+: `el-radio`/`el-checkbox` 用 `value` 非 `label`
- **深色模式**: 禁止硬编码颜色，用 `styles/theme.css` 中的 CSS 变量

## 数据模型

Question 迁移中：废弃 `Options`/`CorrectAnswer`，改用 `QuestionTypeEnum`/`QuestionDataJson`/`Data`
- Entity → DTO: 只映射 `QuestionTypeEnum`, `Data`
- DTO → Entity: 只写入 `QuestionDataJson`
- `QuestionDataJson.type` 使用小写 discriminator: `choice`, `boolean`, `fillBlank`, `shortAnswer`
- 与 `QuestionType` 枚举值对应: `SingleChoice`, `TrueFalse`, `FillBlank`, `ShortAnswer`

## 关键文件

- `Program.cs` - DI、中间件、Serilog
- `Domain/Common/LegacyFieldParser.cs` - 旧字段解析统一入口
- `Infrastructure/Data/AnswerMeDbContext.cs` - DbContext
- `Infrastructure/Services/DataConsistencyCheckService.cs` - 数据一致性检查（启动时自动运行）

## 坑点

- 迁移前停止 API（SQLite 锁定）
- EF 工具失效: `dotnet tool install --global dotnet-ef`
- Windows 下 DLL 锁定: `taskkill /F /PID <pid>` 或 `Stop-Process -Force`
- 数据迁移前备份: `cp answerme.db answerme.db.backup`

## 重要
完成对应任务之后跑一次对应测试比如type-check + 单测 + eslint 验证，确认这轮修复没有引入类型问题
前端注意和风格问题要和其他内容一致，后端注意性能问题，尤其是数据迁移和一致性检查部分