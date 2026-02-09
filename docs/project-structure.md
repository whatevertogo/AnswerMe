# AnswerMe 项目结构

本文档描述 AnswerMe 仓库的目录结构与分层约定（以当前代码为准）。

## 📁 目录结构

```
AnswerMe/
├── .github/workflows/ci-cd.yml       # CI 流水线（格式/测试/Docker 构建校验）
├── backend/                          # 后端（.NET Web API + EF Core）
│   ├── AnswerMe.API/                 # 表现层（Controllers/Filters/启动装配）
│   ├── AnswerMe.Application/         # 应用层（用例服务/DTO/接口抽象）
│   ├── AnswerMe.Domain/              # 领域层（实体/仓储接口）
│   ├── AnswerMe.Infrastructure/      # 基础设施层（DbContext/仓储实现/迁移）
│   ├── AnswerMe.UnitTests/           # 后端单元测试
│   ├── AnswerMe.slnx                 # 解决方案入口
│   └── Dockerfile                    # 后端镜像
├── frontend/                         # 前端（Vue 3 + TypeScript + Pinia + Element Plus）
│   ├── src/
│   │   ├── api/                      # API 封装（Axios）
│   │   ├── stores/                   # Pinia 状态
│   │   ├── router/                   # 路由与守卫
│   │   ├── views/                    # 页面
│   │   └── components/               # 组件
│   ├── package.json                  # 前端脚本与依赖
│   ├── vite.config.ts                # Vite 配置
│   ├── vitest.config.ts              # Vitest 配置
│   ├── Dockerfile                    # 前端镜像
│   └── nginx.conf                    # Nginx 配置
├── docs/                             # 文档（架构/安装/配置/API 等）
├── scripts/                          # 运维/测试辅助脚本
├── docker-compose.yml                # 一键编排（前端/后端/DB）
├── .env.example                      # 环境变量模板
└── README.md                         # 项目概览
```

## 🧱 后端架构（Clean Architecture）

**依赖方向**：API → Application → Domain；Infrastructure → Domain（依赖只能向内流动）。

### 分层职责

- **Domain**（`AnswerMe.Domain/`）：实体与仓储接口（不依赖其他层）
  - `Entities/` - 领域实体
  - `Interfaces/` - 仓储接口（IUserRepository, IQuestionBankRepository 等）

- **Application**（`AnswerMe.Application/`）：用例服务与 DTO（依赖 Domain）
  - `Services/` - 应用服务（业务逻辑）
  - `DTOs/` - 数据传输对象
  - `Interfaces/` - 服务接口
  - `AI/` - AI 抽象（接口、模型、工厂、验证器）
    - `IAIProvider.cs` - AI Provider 接口
    - `AIModels.cs` - 请求/响应 DTO
    - `AIProviderFactory.cs` - Provider 工厂
    - `PromptTemplates.cs` - 提示词模板

- **Infrastructure**（`AnswerMe.Infrastructure/`）：EF Core、仓储实现、AI Provider（依赖 Domain + Application 接口）
  - `Data/` - DbContext 与迁移
  - `Repositories/` - 仓储实现
  - `AI/` - AI Provider 实现（OpenAI, Qwen, Zhipu, Minimax）
  - `DependencyInjection.cs` - DI 扩展方法

- **API**（`AnswerMe.API/`）：控制器、过滤器、认证/授权、组合根（装配依赖）
  - `Controllers/` - HTTP 控制器
  - `Program.cs` - 应用入口与依赖注入

**架构约束验证**：运行 `dotnet test --filter "FullyQualifiedName~Architecture"` 验证分层规则。

说明：后端目标框架以各 `*.csproj` 为准（当前为 `net10.0`）。

## 🎨 前端架构

- 框架：Vue 3（Composition API）+ TypeScript
- 状态：Pinia（按业务域拆分 store）
- API：`src/api/*` 统一封装请求
- 页面：`src/views/*` 聚合业务页面

## ✅ CI/CD

CI 主要做三件事：
- 后端：格式检查 + 构建 + 单元测试
- 前端：Lint（不自动修复）+ 类型检查 + 单元测试
- Docker：仅做镜像构建校验，不依赖推送凭据
