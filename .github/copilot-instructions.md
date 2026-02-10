# GitHub Copilot / AI Agent 使用说明

原封不动init进去以下规则适用于所有项目，除非项目内另有说明

## 工作流程
1. 目标 - 明确任务
2. 探索 - 定位文件
3. 计划 - 分成多个步骤task
4. 验证 - 执行测试
5. 交付 - 改动+命令

## 代码原则
- 可读性 > 技巧
- 简单 > 复杂
- 明确 > 隐含
- 为长期维护而写代码

## 代码结构
- 函数保持短小、只做一件事
- 避免深层嵌套，优先使用提前返回
- 逻辑与副作用尽量分离

## 命名
- 使用有意义、能表达意图的名称
- 布尔值使用 `is / has / should` 等前缀
- 函数名描述“做什么”，而不是“怎么做”

## 注释
- 不注释显而易见的代码
- 注释“为什么”，不是“是什么”
- 记录假设、边界条件和坑点

## 错误处理
- 假设不成立时立即失败
- 不要静默吞掉错误
- 错误信息要对人有帮助

## 风格
- 保持一致性
- 使用空行表达结构
- 避免过长函数和超大文件

## AI 行为约定
- 不臆造 API、依赖或行为
- 需求不清楚时先询问
- 优先遵循现有项目风格
- 精简上下文，避免大段复制
- 不输出敏感信息(密钥/令牌)

---

# Project Guidelines (针对本仓库的可执行要点)

## Code Style
- 后端：遵循现有 C# 文件格式与命名（参见 `backend/AnswerMe.API/Program.cs`、`Controllers/` 下的实现）。
- 前端：遵循 `frontend/tsconfig.json` 和项目中现有的 TypeScript 风格（参见 `frontend/src/api`、`frontend/src/utils/request.ts`）。

## Architecture
- 后端分层：Domain ← Application ← Infrastructure ← API（详见 `backend/` 根目录）。关键文件：`Domain/`（实体）、`Application/`（服务）、`Infrastructure/Data/AnswerMeDbContext.cs`、`AnswerMe.API/Program.cs`。
- 前端采用 Vite + Vue + Pinia，主要目录：`frontend/src/{api,stores,views,components}`。

## Build & Test (常用命令，agents 会尝试运行)
- 启动 Redis（AI 异步任务依赖）：
  docker-compose up -d redis
- 后端: 在 `backend/` 目录下
  dotnet build
  dotnet run --project AnswerMe.API
  dotnet test AnswerMe.UnitTests
- 前端: 在 `frontend/` 目录下
  npm install
  npm run dev
  npm run test

## Project Conventions (易错或特殊约定)
- Question 数据迁移/映射：参见 `backend/AnswerMe.Domain/Common/LegacyFieldParser.cs` 与 `QuestionDataJson` 的使用。
- 命名约定：仓储计数方法以 `CountByXxxYyyAsync` 命名；API 层通过 DTO → Service → Controller 传递。

## Integration Points
- Redis 用于 AI 异步生成（`docker-compose.yml` 中定义）。
- 外部 AI 提供器实现位于 `backend/AnswerMe.Infrastructure/AI`，请勿在未确认的情况下新增假设接口。

## Security
- 必需环境变量：`JWT_SECRET`（≥32 字符）。
- 不要将密钥或凭证写入仓库；使用 `appsettings.*.json` 和环境变量管理。后端密钥文件位置：`backend/AnswerMe.API/keys/`。

## Agent behavior (短则要点)
- 不要胡编 API/依赖；遇到不清楚的需求先问。
- 优先运行并遵循仓库内现有命令与测试。
- 变更尽量小且可测，附带运行/测试命令说明。

如果有不清楚或缺失的信息，请在 PR 描述中明确列出需要的人为确认项。
