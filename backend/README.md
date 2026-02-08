# AnswerMe Backend 项目结构

## 技术栈
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
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
│   ├── Validators/            # FluentValidation验证器
│   └── Mappings/              # AutoMapper配置
│
├── AnswerMe.Infrastructure/   # 基础设施层(外部依赖)
│   ├── Data/                  # EF Core(DbContext, Migrations)
│   ├── AI/                    # AI Provider实现(OpenAI, Qwen等)
│   └── Services/              # 基础设施服务(加密、邮件等)
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
- ✅ Infrastructure层实现Application层定义的接口
- ✅ API层协调Application和Infrastructure层

## 项目启动

### 开发环境
```bash
cd backend
dotnet restore
dotnet build
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
- 数据库连接字符串
- JWT密钥
- 日志级别
- API限流配置

## API端点
- `POST /api/auth/register` - 用户注册
- `POST /api/auth/login` - 用户登录
- `GET /api/questionbanks` - 获取题库列表
- `POST /api/questions/generate` - AI生成题目
- `GET /health` - 健康检查

详细API文档参见 `/docs/openapi/specs/` 目录
