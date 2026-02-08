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

- Docker 20.10+
- Docker Compose 2.0+

### 一键启动

```bash
# 1. 克隆仓库
git clone https://github.com/whatevetogo/Answerme.git
cd ai-questionbank

# 2. 配置环境变量
cp .env.example .env
# 编辑 .env 文件,设置必要的环境变量

# 3. 启动服务(后台运行)
docker-compose up -d

# 4. 查看日志
docker-compose logs -f

# 5. 访问应用
# 前端: http://localhost:3000
# 后端API: http://localhost:5000/swagger
```

### 首次使用

1. 打开浏览器访问 `http://localhost:3000`
2. 注册账户(或在 `.env` 中设置 `ENABLE_REGISTRATION=false` 进入单用户模式)
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

## 🛠️ 技术栈

### 后端
- **.NET 8** - 跨平台高性能框架
- **Entity Framework Core** - ORM数据访问
- **SQLite / PostgreSQL** - 数据库
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
- [ ] 核心功能开发
- [ ] Docker部署配置
- [ ] 文档编写
- [ ] 测试覆盖
- [ ] 首次发布

查看 [任务列表](openspec/changes/ai-questionbank-mvp/tasks.md) 了解详细开发计划。

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
