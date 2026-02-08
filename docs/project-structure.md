# AI Question Bank 项目结构

本文档描述AI Question Bank项目的完整目录结构和文件说明。

## 📁 目录结构

```
ai-questionbank/
├── .github/
│   └── workflows/
│       └── ci-cd.yml                 # GitHub Actions CI/CD流水线
│
├── backend/                          # .NET 8 后端API
│   ├── AIQuestionBank.API/           # 主API项目
│   │   ├── Controllers/              # API控制器
│   │   ├── Models/                   # 数据模型
│   │   ├── Services/                 # 业务逻辑服务
│   │   ├── Data/                     # 数据访问层
│   │   ├── Middleware/               # 中间件
│   │   ├── Utils/                    # 工具类
│   │   └── appsettings.json          # 应用配置
│   │
│   ├── AIQuestionBank.Core/          # 核心领域层
│   ├── AIQuestionBank.Infrastructure/# 基础设施层
│   ├── AIQuestionBank.Tests/         # 单元测试
│   └── Dockerfile                    # 后端Docker镜像
│
├── frontend/                         # Vue 3 前端应用
│   ├── src/
│   │   ├── assets/                   # 静态资源
│   │   ├── components/               # 可复用组件
│   │   ├── views/                    # 页面组件
│   │   ├── stores/                   # Pinia状态管理
│   │   ├── services/                 # API服务
│   │   ├── types/                    # TypeScript类型
│   │   ├── router/                   # 路由配置
│   │   ├── App.vue                   # 根组件
│   │   └── main.ts                   # 入口文件
│   │
│   ├── public/                       # 公共静态文件
│   ├── package.json                  # 依赖配置
│   ├── vite.config.ts                # Vite配置
│   ├── Dockerfile                    # 前端Docker镜像
│   └── nginx.conf                    # Nginx配置
│
├── docs/                             # 项目文档
│   ├── installation.md               # 安装部署指南
│   ├── configuration.md              # 环境变量配置
│   ├── api.md                        # API接口文档
│   ├── faq.md                        # 常见问题
│   ├── deployment-checklist.md       # 部署检查清单
│   ├── architecture/                 # 架构文档
│   │   ├── architecture-exploration.md
│   │   └── risk-analysis.md
│   ├── UX-DESIGN-ANALYSIS.md         # UX设计分析
│   └── UX-FLOWCHARTS.md              # 用户流程图
│
├── scripts/                          # 实用脚本
│   ├── wait-for-health.sh            # 健康检查等待脚本
│   ├── backup.sh                     # 数据备份脚本
│   ├── restore.sh                    # 数据恢复脚本
│   └── migrate-sqlite-to-postgres.sh # 数据库迁移脚本
│
├── openspec/                         # OpenSpec工作流配置
│   ├── config.yaml
│   └── changes/                      # 功能变更记录
│       └── ai-questionbank-mvp/
│           ├── .openspec.yaml
│           ├── proposal.md
│           ├── design.md
│           ├── tasks.md
│           └── specs/                # 功能规格说明
│               ├── user-auth/
│               ├── question-bank-management/
│               ├── ai-question-generation/
│               ├── api-key-security/
│               ├── data-export-import/
│               └── deployment-experience/
│
├── .claude/                          # Claude Code配置
│   ├── skills/                       # 技能定义
│   └── commands/                     # 命令定义
│
├── .env.example                      # 环境变量模板
├── .gitignore                        # Git忽略配置
├── docker-compose.yml                # Docker Compose配置
├── README.md                         # 项目说明文档
├── QUICKSTART.md                     # 快速开始指南
├── CONTRIBUTING.md                   # 贡献指南
└── LICENSE                           # MIT许可证
```

## 📄 核心文件说明

### 配置文件

| 文件 | 说明 |
|------|------|
| `.env.example` | 环境变量配置模板,复制为.env后修改 |
| `docker-compose.yml` | Docker Compose编排文件,定义所有服务 |
| `.gitignore` | Git版本控制忽略规则 |
| `LICENSE` | MIT开源许可证 |

### 文档文件

| 文件 | 说明 |
|------|------|
| `README.md` | 项目主文档,包含功能介绍和快速开始 |
| `QUICKSTART.md` | 5分钟快速部署指南 |
| `CONTRIBUTING.md` | 贡献者指南,包含开发规范 |
| `docs/installation.md` | 详细安装部署文档 |
| `docs/configuration.md` | 完整环境变量配置参考 |
| `docs/api.md` | RESTful API接口文档 |
| `docs/faq.md` | 常见问题解答 |
| `docs/deployment-checklist.md` | 生产环境部署检查清单 |

### 脚本文件

| 脚本 | 说明 |
|------|------|
| `scripts/wait-for-health.sh` | 等待服务健康检查通过 |
| `scripts/backup.sh` | 自动备份PostgreSQL数据库 |
| `scripts/restore.sh` | 恢复数据库备份 |
| `scripts/migrate-sqlite-to-postgres.sh` | SQLite到PostgreSQL迁移 |

### Docker文件

| 文件 | 说明 |
|------|------|
| `backend/Dockerfile` | 后端.NET 8 API Docker镜像 |
| `frontend/Dockerfile` | 前端Vue 3 Nginx Docker镜像 |
| `frontend/nginx.conf` | Nginx反向代理配置 |

### CI/CD文件

| 文件 | 说明 |
|------|------|
| `.github/workflows/ci-cd.yml` | GitHub Actions自动化流水线 |

## 🔧 后端架构

### 分层架构

```
AIQuestionBank.API/              # 表现层
├── Controllers/                  # API端点
├── Filters/                      # 过滤器
├── Middleware/                   # 中间件
└── DTOs/                        # 数据传输对象

AIQuestionBank.Core/              # 领域层
├── Entities/                     # 实体
├── Interfaces/                   # 接口
├── Services/                     # 领域服务
└── ValueObjects/                 # 值对象

AIQuestionBank.Infrastructure/    # 基础设施层
├── Data/                         # 数据访问
│   ├── ApplicationDbContext.cs
│   └── Repositories/             # 仓储实现
├── Services/                     # 基础设施服务
│   ├── AIProviders/              # AI Provider实现
│   └── Encryption/               # 加密服务
└── Migrations/                   # EF Core迁移
```

### 关键组件

- **Controllers**: 处理HTTP请求,调用服务层
- **Services**: 业务逻辑实现
- **Repositories**: 数据访问抽象
- **AI Providers**: AI服务抽象层
- **Middleware**: 认证、日志、异常处理

## 🎨 前端架构

### 目录结构

```
src/
├── assets/                       # 静态资源
│   ├── images/                   # 图片
│   └── styles/                   # 全局样式
│
├── components/                   # 可复用组件
│   ├── common/                   # 通用组件
│   │   ├── Button.vue
│   │   ├── Input.vue
│   │   └── Modal.vue
│   └── business/                 # 业务组件
│       ├── QuestionCard.vue
│       └── QuestionBankCard.vue
│
├── views/                        # 页面组件
│   ├── Home.vue                  # 首页
│   ├── Login.vue                 # 登录页
│   ├── Register.vue              # 注册页
│   ├── QuestionBanks.vue         # 题库列表
│   ├── QuestionBankDetail.vue    # 题库详情
│   ├── Practice.vue              # 答题页
│   └── Settings.vue              # 设置页
│
├── stores/                       # Pinia状态管理
│   ├── user.ts                   # 用户状态
│   ├── questions.ts              # 题目状态
│   └── ai.ts                     # AI配置状态
│
├── services/                     # API服务
│   ├── api.ts                    # API基础配置
│   ├── auth.ts                   # 认证API
│   ├── questionBanks.ts          # 题库API
│   ├── questions.ts              # 题目API
│   └── ai.ts                     # AI生成API
│
├── router/                       # 路由配置
│   └── index.ts                  # 路由定义
│
├── types/                        # TypeScript类型
│   ├── user.ts
│   ├── question.ts
│   └── ai.ts
│
├── utils/                        # 工具函数
│   ├── request.ts                # HTTP请求封装
│   ├── storage.ts                # 本地存储封装
│   └── helpers.ts                # 辅助函数
│
├── App.vue                       # 根组件
└── main.ts                       # 应用入口
```

### 技术栈

- **框架**: Vue 3 (Composition API)
- **构建工具**: Vite
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **UI库**: Element Plus
- **HTTP客户端**: Axios
- **类型**: TypeScript

## 🗄️ 数据库设计

### 核心表

```sql
-- 用户表
users (
    id UUID PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 题库表
question_banks (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    tags JSONB,
    version INT DEFAULT 1,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

-- 题目表
questions (
    id UUID PRIMARY KEY,
    bank_id UUID REFERENCES question_banks(id),
    type VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    options JSONB,
    correct_answer TEXT,
    explanation TEXT,
    difficulty VARCHAR(20),
    tags JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

-- AI配置表
user_ai_configs (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    provider VARCHAR(50) NOT NULL,
    api_key_encrypted TEXT NOT NULL,
    model VARCHAR(100),
    api_base TEXT,
    created_at TIMESTAMP DEFAULT NOW()
);

-- 答题记录表
answer_records (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES users(id),
    question_id UUID REFERENCES questions(id),
    user_answer TEXT,
    is_correct BOOLEAN,
    time_spent INT,
    answered_at TIMESTAMP DEFAULT NOW(),
    UNIQUE(user_id, question_id)
);
```

## 🔐 安全特性

- **认证**: JWT Token认证
- **密码**: bcrypt加密(10轮)
- **API密钥**: AES-256加密存储
- **输入验证**: 数据注解验证
- **SQL注入**: EF Core参数化查询
- **XSS**: 前端输入转义
- **CORS**: 配置允许的来源
- **HTTPS**: 生产环境强制

## 🚀 部署架构

```
┌─────────────────────────────────────┐
│         Nginx (反向代理)             │
│     (SSL终止, 静态文件服务)          │
└─────────────────────────────────────┘
           │              │
           ▼              ▼
┌─────────────────┐  ┌─────────────────┐
│   Frontend      │  │    Backend      │
│  (Vue 3 +       │  │   (.NET 8 API)  │
│   Nginx)        │  │                 │
└─────────────────┘  └────────┬────────┘
                             │
                             ▼
                    ┌─────────────────┐
                    │  PostgreSQL     │
                    │   (数据库)       │
                    └─────────────────┘
```

## 📊 数据流

```
用户 → Frontend (Vue 3)
        ↓ HTTP/JSON
    Backend API (.NET 8)
        ↓
    业务逻辑层
        ↓
    数据访问层 (EF Core)
        ↓
    PostgreSQL数据库
```

## 🔧 开发流程

### 1. 功能开发

```
OpenSpec规格说明
    ↓
设计评审
    ↓
开发实现
    ↓
单元测试
    ↓
集成测试
    ↓
代码审查
    ↓
合并到主分支
```

### 2. 部署流程

```
代码提交
    ↓
CI/CD流水线
    ↓
构建镜像
    ↓
运行测试
    ↓
部署到开发环境
    ↓
验证测试
    ↓
部署到生产环境
```

## 📚 扩展阅读

- [架构设计文档](docs/architecture/architecture-exploration.md)
- [风险评估](docs/architecture/risk-analysis.md)
- [任务列表](openspec/changes/ai-questionbank-mvp/tasks.md)
- [API文档](docs/api.md)

---

**最后更新**: 2024-01-01
**维护者**: AI Question Bank Team
