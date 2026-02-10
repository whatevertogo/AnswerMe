# AI Question Bank

<div align="center">

**智能题库系统 - 自托管·数据掌控·AI驱动**

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Vue](https://img.shields.io/badge/Vue-3.5-green.svg)](https://vuejs.org/)
[![Docker](https://img.shields.io/badge/Docker-Compose-blue.svg)](https://www.docker.com/)

一个用户自托管的智能题库系统,使用您自己的AI API密钥生成题目,完全掌控数据和隐私。

[快速开始](#-快速开始) · [功能特性](#-功能特性) · [部署文档](docs/installation.md) · [API文档](docs/api.md) · [常见问题](docs/faq.md)

</div>

---

## ✨ 功能特性

### 核心功能

- **🤖 AI驱动题目生成** - 支持OpenAI、通义千问等多个AI Provider,自定义生成参数
- **🔐 完全数据掌控** - 所有数据存储在本地,支持导出导入,隐私完全可控
- **🎯 多种题型支持** - 单选题、多选题、判断题、填空题、简答题
- **📊 智能学习追踪** - 答题记录、错题本、学习统计分析
- **🔒 API密钥安全** - AES-256加密存储,密钥永不泄露给前端
- **📦 一键部署** - Docker Compose 5分钟快速部署

### 技术亮点

- **单体架构** - 简化部署,降低运维成本
- **数据库灵活** - 开发环境用SQLite,生产环境用PostgreSQL
- **现代技术栈** - .NET 8 + Vue 3 + Element Plus
- **安全优先** - JWT认证、API密钥加密、SQL注入防护
- **可扩展设计** - 清晰的分层架构,易于添加新功能

## 🚀 快速开始

### 环境要求

- .NET 10 SDK
- Node.js 18+
- npm 或 pnpm
- Docker（可选，用于 PostgreSQL + Redis）

### 一键启动

#### 方式一：本地开发服务器（推荐开发）

```bash
# 1. 克隆仓库
git clone https://github.com/whatevetogo/Answerme.git
cd AnswerMe

# 2. 配置环境变量
cp .env.example .env
# 编辑 .env 文件，设置必要的环境变量

# 3. 启动依赖服务（Redis 必需）
docker-compose up -d redis
# AI 异步生成功能依赖 Redis，后端启动会检查 Redis 连接

# 4. 启动后端服务器（终端1）
cd backend
dotnet run --project AnswerMe.API
# 后端将运行在 http://localhost:5000（或配置的端口）

# 5. 启动前端开发服务器（终端2）
cd frontend
npm install           # 首次运行需要安装依赖
npm run dev           # 启动 Vite 开发服务器
# 前端将运行在 http://localhost:5173（或可用端口）

# 6. 访问应用
# 打开浏览器访问前端地址（如 http://localhost:5173）
```

#### 方式二：Docker Compose（推荐生产）

```bash
# 1. 配置环境变量
cp .env.example .env
# 编辑 .env 文件，设置必要的环境变量

# 2. 启动服务（后台运行）
docker-compose up -d

# 3. 查看日志
docker-compose logs -f

# 4. 访问应用
# 前端: http://localhost:3000
# 后端API: http://localhost:5000/swagger
```

### 本地开发命令汇总

**后端（.NET 10 + ASP.NET Core）**:
```bash
cd backend
dotnet run --project AnswerMe.API              # 启动开发服务器（热重载）
dotnet build AnswerMe.API                      # 编译项目
dotnet test                                    # 运行测试
```

**前端（Vue 3 + Vite）**:
```bash
cd frontend
npm install                                    # 安装依赖
npm run dev                                    # 启动开发服务器（http://localhost:5173）
npm run build                                  # 生产构建
npm run preview                                # 预览生产构建
npm run test                                   # 运行测试
npm run lint                                   # ESLint 检查和修复
```

### 首次使用

1. 打开浏览器访问前端地址（本地开发为 `http://localhost:5173`）
2. 使用本地登录功能（已在 .env 中预配置）
   - 用户名: `LocalUser`
   - 密码: `local123`
3. 在 **设置 → AI配置** 中添加您的AI API密钥
4. 创建题库并生成您的第一批AI题目!

## 📖 文档

- [安装部署指南](docs/installation.md) - 详细部署步骤和配置说明
- [环境变量配置](docs/configuration.md) - 所有配置项说明
- [API文档](docs/api.md) - REST API接口文档
- [常见问题](docs/faq.md) - 问题排查和解决方案
- [贡献指南](CONTRIBUTING.md) - 如何参与项目贡献

## 🎯 使用场景

- **个人学习** - 创建专属题库,AI辅助生成练习题
- **教育培训** - 教师快速生成考试题目,自动化出题
- **企业培训** - 企业内部培训题库管理,知识考核
- **开源社区** - 团队协作构建共享题库

## 💻 开发命令

### 后端（.NET 10）

```bash
# 导航到后端目录
cd backend

# 启动开发服务器（支持热重载）
dotnet run --project AnswerMe.API
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger

# 编译项目
dotnet build

# 运行测试
dotnet test

# 应用数据库迁移
dotnet ef database update --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# 创建新迁移
dotnet ef migrations add MigrationName --project AnswerMe.Infrastructure --startup-project AnswerMe.API

# 监视模式运行（自动重启）
dotnet watch --project AnswerMe.API
```

### 前端（Vue 3 + Vite）

```bash
# 导航到前端目录
cd frontend

# 首次运行 - 安装依赖
npm install

# 启动开发服务器（热重载）
npm run dev
# 默认: http://localhost:5173
# 如果端口被占用，会自动尝试 5174, 5175...

# 生产构建
npm run build
# 输出: dist/

# 预览生产构建
npm run preview

# 运行测试
npm run test                # 运行测试
npm run test:ui            # 测试 UI 界面
npm run test:coverage      # 测试覆盖率报告

# 代码质量
npm run lint               # ESLint 检查和自动修复
npm run format             # Prettier 格式化
```

### Docker Compose

```bash
# 启动所有服务（db + backend）
docker-compose up -d

# 查看日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f backend
docker-compose logs -f db

# 停止服务
docker-compose down

# 停止并删除数据卷
docker-compose down -v

# 重建并启动
docker-compose up -d --build

# 进入后端容器
docker-compose exec backend bash

# 进入数据库容器
docker-compose exec db psql -U answeruser -d answermedb
```

### 端口配置

| 服务 | 默认端口 | 环境变量 | 说明 |
|------|---------|---------|------|
| 前端 | 5173 | `FRONTEND_PORT` | Vite 开发服务器（自动寻找可用端口） |
| 后端 API | 5000 | `BACKEND_PORT` | ASP.NET Core API |
| 数据库 | 5432 | `POSTGRES_PORT` | PostgreSQL（Docker 模式） |
| Redis | 6379 | `REDIS_PORT` | Redis 任务队列（Docker 模式） |

**注意**: Vite 开发服务器如果默认端口被占用，会自动尝试 5174、5175 等端口。

### 环境变量速查

**必需配置**（首次运行前设置）:
```bash
# JWT 密钥（至少32字符）
JWT_SECRET=your-secret-key-minimum-32-characters-long

# 数据库密码（生产环境）
POSTGRES_PASSWORD=your-secure-password
```

**可选配置**（开发默认值）:
```bash
# 本地认证模式（个人使用）
LOCAL_AUTH__ENABLE_LOCAL_LOGIN=true
LOCAL_AUTH__DEFAULT_USERNAME=LocalUser
LOCAL_AUTH__DEFAULT_PASSWORD=local123

# 前端地址（CORS配置）
ALLOWED_ORIGINS=http://localhost:5173,http://localhost:3000
```

完整配置选项请查看 `.env.example` 文件。

## 🛠️ 技术栈

### 后端
- **.NET 10** - 跨平台高性能框架
- **Entity Framework Core** - ORM数据访问
- **SQLite / PostgreSQL** - 数据库
- **Redis** - AI 异步任务队列
- **ASP.NET Core Identity** - 用户认证
- **Swashbuckle** - Swagger API文档

### 前端
- **Vue 3** - 渐进式JavaScript框架
- **Vite** - 快速构建工具
- **TypeScript** - 类型安全
- **Element Plus** - UI组件库
- **Pinia** - 状态管理
- **Vue Router** - 路由管理

### DevOps
- **Docker** - 容器化
- **Docker Compose** - 服务编排
- **GitHub Actions** - CI/CD

### DevOps
- **Docker** - 容器化
- **Docker Compose** - 服务编排
- **GitHub Actions** - CI/CD

## 🔐 安全特性

- ✅ API密钥AES-256加密存储
- ✅ JWT Token认证
- ✅ 密码bcrypt加密(10轮)
- ✅ SQL注入防护
- ✅ XSS攻击防护
- ✅ API速率限制
- ✅ CORS策略配置
- ✅ 强制HTTPS(生产环境)

## 📊 项目状态

**当前版本**: v0.1.0-alpha

**开发进度**:
- [x] 项目架构设计
- [x] 核心功能开发
- [x] Docker部署配置
- [x] 基础文档编写
- [ ] 测试覆盖
- [ ] 首次发布

查看 [任务列表](openspec/changes/ai-questionbank-mvp/tasks.md) 了解详细开发计划。

## ⚠️ 已知问题

当前 v0.1.0-alpha 版本存在以下已知问题:

### 功能限制

- **数据导入功能**: 暂未实现,仅支持导出。如需迁移数据,可直接备份SQLite数据库文件。

### Redis 配置（AI 异步生成）

**AI 生成题目支持两种模式：**
- **同步模式**：题目数量 ≤ 20，直接返回结果
- **异步模式**：题目数量 > 20，后台生成，通过 Redis 队列处理

**Redis 是必需的** - 后端启动时会检查 Redis 连接，如果连接失败将拒绝启动。

**启动 Redis（Docker 推荐）：**
```bash
docker-compose up -d redis
```

**本地安装 Redis：**
- Windows: 下载 [Redis for Windows](https://github.com/microsoftarchive/redis/releases) 或使用 WSL
- macOS: `brew install redis && brew services start redis`
- Linux: `sudo systemctl start redis`

### 安全建议

- **JWT Token存储**: 当前使用localStorage存储Token,存在XSS理论风险。建议在生产环境使用httpOnly cookie。
- **本地登录端点**: `/api/auth/local-login` 端点仅用于开发测试,生产环境应在 `.env` 中设置 `ENABLE_LOCAL_AUTH=false` 禁用。

### 数据备份

**手动备份SQLite数据库** (开发环境):
```bash
# 备份当前数据库文件
cp backend/answerme_dev.db backups/answerme_$(date +%Y%m%d_%H%M%S).db
```

**手动备份PostgreSQL数据库** (生产环境):
```bash
# 使用Docker卷备份
docker-compose exec db pg_dump -U answerme answerme > backup_$(date +%Y%m%d).sql

# 或直接备份整个数据卷
docker run --rm \
  -v answerme_db_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/answerme_db_$(date +%Y%m%d).tar.gz -C /data .
```

**恢复备份**:
```bash
# SQLite恢复
cp backups/answerme_20250209.db backend/answerme_dev.db

# PostgreSQL恢复
docker-compose exec -T db psql -U answerme answerme < backup_20250209.sql
```

## 🤝 贡献

我们欢迎所有形式的贡献!

- 报告Bug
- 讨论代码状态
- 提交修复
- 提出新功能
- 成为维护者

请阅读 [贡献指南](CONTRIBUTING.md) 了解详情。

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

## 🌟 致谢

感谢所有为本项目做出贡献的开发者!

---

<div align="center">

**如果这个项目对您有帮助,请给我们一个⭐️**

Made with ❤️ by Open Source Community

</div>
