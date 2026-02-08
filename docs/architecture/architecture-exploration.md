# AnswerMe 系统架构探索文档

## 1. 架构概览

### 1.1 系统定位
- **目标**：开源自托管AI题库系统
- **核心特色**：用户配置自己的API（AI或任何HTTP API）生成题目
- **技术栈**：.NET 8 后端 + Vue 3 前端 + PostgreSQL
- **部署方式**：Docker Compose 单机部署

### 1.2 架构选型：单体应用

**决策理由**：
| 维度 | 单体应用 | 微服务 | 选择 |
|------|---------|--------|------|
| 开发效率 | ✅ 高 | ❌ 低（服务间协调） | 单体 |
| 部署复杂度 | ✅ 简单（1个容器） | ❌ 复杂（多个容器+网络） | 单体 |
| 性能要求 | ✅ MVP阶段足够 | ⚠️ 过度设计 | 单体 |
| 团队规模 | ✅ 适合小团队 | ❌ 需要更多协调 | 单体 |
| 自托管场景 | ✅ 用户友好 | ❌ 维护成本高 | 单体 |
| 扩展性 | ⚠️ 后期可拆分 | ✅ 天然支持 | 单体（未来可拆分） |

**结论**：MVP阶段选择单体应用，满足当前需求，降低复杂度。

---

## 2. 架构设计

### 2.1 分层架构图

```
┌─────────────────────────────────────────────────────┐
│                  Frontend (Vue 3)                   │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │  Dashboard  │  │  Question   │  │   Settings  │ │
│  │   Views     │  │  Generator  │  │  & Config   │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
│           ↓ Pinia Store (State Management)          │
└─────────────────────┬───────────────────────────────┘
                      │ HTTP (JWT)
                      ↓
┌─────────────────────────────────────────────────────┐
│              Backend (.NET 8 Web API)               │
│  ┌───────────────────────────────────────────────┐  │
│  │         Controllers (API Endpoints)           │  │
│  │  /auth  /questions  /attempts  /datasources  │  │
│  └───────────────────┬───────────────────────────┘  │
│                      ↓                               │
│  ┌───────────────────────────────────────────────┐  │
│  │         Application Services                  │  │
│  │  QuestionService  AttemptService  AuthService │  │
│  └───────────────────┬───────────────────────────┘  │
│                      ↓                               │
│  ┌───────────────────────────────────────────────┐  │
│  │         Domain Layer (Core Logic)             │  │
│  │  Entities  Value Objects  Domain Services     │  │
│  └───────────────────┬───────────────────────────┘  │
│                      ↓                               │
│  ┌───────────────────────────────────────────────┐  │
│  │         Infrastructure                         │  │
│  │  ┌─────────────┐  ┌─────────────────────┐    │  │
│  │  │  EF Core    │  │   AI Providers      │    │  │
│  │  │  (Postgres) │  │  - OpenAI           │    │  │
│  │  │             │  │  - Custom API       │    │  │
│  │  │             │  │  - HttpClient       │    │  │
│  │  └─────────────┘  └─────────────────────┘    │  │
│  └───────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────┘
                      ↓
┌─────────────────────────────────────────────────────┐
│              External Services                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │  OpenAI  │  │  Qwen    │  │  User Custom API │  │
│  └──────────┘  └──────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────┘
```

### 2.2 核心技术决策

| 决策点 | 方案A（选择） | 方案B | 理由 |
|--------|-------------|-------|------|
| **数据库** | PostgreSQL | MongoDB | 关系型适合事务一致性（答题记录），JSONB支持灵活配置 |
| **前端状态** | Pinia | Vuex | Vue 3官方推荐，TS支持更好 |
| **API风格** | REST | GraphQL | REST足够简单，自托管不需要复杂查询 |
| **AI调用** | 服务端统一调用 | 前端直调 | 保护API Key，统一错误处理，便于监控 |
| **认证方式** | JWT | Session | 无状态，适合REST，前后端分离友好 |

---

## 3. 数据库设计

### 3.1 实体关系图（ER）

```
┌──────────────┐
│    Users     │
│--------------│
│ Id (PK)      │
│ Username     │
│ Email        │
│ PasswordHash │
│ CreatedAt    │
└──────┬───────┘
       │ 1
       │
       │ N
┌──────▼─────────────┐         ┌──────────────────┐
│   DataSources      │         │  QuestionBanks   │
│--------------------│         │------------------│
│ Id (PK)            │         │ Id (PK)          │
│ UserId (FK)        │         │ UserId (FK)      │
│ Name               │         │ Name             │
│ Type (enum)        │         │ Description      │
│ Config (JSONB)     │         │ DataSourceId(FK) │
│ IsDefault          │         │ Tags (JSONB)     │
│ CreatedAt          │         │ CreatedAt        │
└────────────────────┘         └─────────┬────────┘
                                         │ 1
                                         │
                                         │ N
         ┌──────────────┐       ┌────────▼─────┐
         │   Attempts   │       │  Questions   │
         │--------------│       │--------------│
         │ Id (PK)      │       │ Id (PK)      │
         │ UserId (FK)  │       │ QuestionBank │
         │ QuestionBank │       │   Id (FK)    │
         │   Id (FK)    │       │ QuestionText │
         │ StartedAt    │       │ QuestionType │
         │ CompletedAt  │       │ Options(JSON)│
         │ Score        │       │ CorrectAnswer│
         └──────┬───────┘       │ Difficulty   │
                │               └──────────────┘
                │ 1
                │
                │ N
         ┌──────▼──────────┐
         │ AttemptDetails  │
         │-----------------│
         │ Id (PK)         │
         │ AttemptId (FK)  │
         │ QuestionId (FK) │
         │ UserAnswer      │
         │ IsCorrect       │
         │ TimeSpent       │
         └─────────────────┘
```

### 3.2 核心表设计

#### 3.2.1 Users（用户表）
```sql
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON Users(Email);
```

#### 3.2.2 DataSources（数据源配置表）
```sql
CREATE TABLE DataSources (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Name VARCHAR(100) NOT NULL,
    Type VARCHAR(20) NOT NULL, -- 'openai', 'claude', 'qwen', 'custom_api'
    Config JSONB NOT NULL,
    IsDefault BOOLEAN DEFAULT FALSE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_datasources_user ON DataSources(UserId);
CREATE INDEX idx_datasources_type ON DataSources(Type);
```

**Config字段示例**：
```json
// OpenAI配置
{
  "apiKey": "sk-xxx",
  "baseUrl": "https://api.openai.com/v1",
  "model": "gpt-4"
}

// 自定义API配置
{
  "endpoint": "https://api.example.com/generate",
  "method": "POST",
  "headers": {
    "Authorization": "Bearer {token}",
    "Content-Type": "application/json"
  },
  "requestTemplate": {
    "prompt": "{prompt}",
    "count": {count}
  },
  "responseMapping": {
    "questionsPath": "$.data.questions",
    "questionText": "$.text",
    "options": "$.options",
    "correctAnswer": "$.answer"
  }
}
```

#### 3.2.3 QuestionBanks（题库表）
```sql
CREATE TABLE QuestionBanks (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    Name VARCHAR(200) NOT NULL,
    Description TEXT,
    DataSourceId INTEGER REFERENCES DataSources(Id) ON DELETE SET NULL,
    Tags JSONB DEFAULT '[]',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_questionbanks_user ON QuestionBanks(UserId);
CREATE INDEX idx_questionbanks_datasource ON QuestionBanks(DataSourceId);
```

#### 3.2.4 Questions（题目表）
```sql
CREATE TABLE Questions (
    Id SERIAL PRIMARY KEY,
    QuestionBankId INTEGER NOT NULL REFERENCES QuestionBanks(Id) ON DELETE CASCADE,
    QuestionText TEXT NOT NULL,
    QuestionType VARCHAR(20) NOT NULL, -- 'choice', 'fill_blank', 'essay'
    Options JSONB, -- 选择题选项: ["A. xxx", "B. xxx"]
    CorrectAnswer TEXT NOT NULL,
    Explanation TEXT,
    Difficulty VARCHAR(10) DEFAULT 'medium', -- 'easy', 'medium', 'hard'
    OrderIndex INTEGER NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_questions_bank ON Questions(QuestionBankId);
CREATE INDEX idx_questions_type ON Questions(QuestionType);
```

#### 3.2.5 Attempts（答题记录表）
```sql
CREATE TABLE Attempts (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    QuestionBankId INTEGER NOT NULL REFERENCES QuestionBanks(Id) ON DELETE CASCADE,
    StartedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP,
    Score NUMERIC(5,2),
    TotalQuestions INTEGER NOT NULL
);

CREATE INDEX idx_attempts_user ON Attempts(UserId);
CREATE INDEX idx_attempts_bank ON Attempts(QuestionBankId);
```

#### 3.2.6 AttemptDetails（答题详情表）
```sql
CREATE TABLE AttemptDetails (
    Id SERIAL PRIMARY KEY,
    AttemptId INTEGER NOT NULL REFERENCES Attempts(Id) ON DELETE CASCADE,
    QuestionId INTEGER NOT NULL REFERENCES Questions(Id) ON DELETE CASCADE,
    UserAnswer TEXT,
    IsCorrect BOOLEAN,
    TimeSpent INTEGER, -- 秒
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_attemptdetails_attempt ON AttemptDetails(AttemptId);
CREATE INDEX idx_attemptdetails_question ON AttemptDetails(QuestionId);
```

### 3.3 数据库设计决策

| 问题 | 方案A（JSON字段） | 方案B（独立表） | 选择 | 理由 |
|------|------------------|----------------|------|------|
| **用户AI配置** | DataSources.Config | UserAIConfigs表 | 方案A | 灵活，不同Provider配置差异大 |
| **题目选项** | Questions.Options | QuestionOptions表 | 方案A | 选项结构简单，不需要关联查询 |
| **题库标签** | QuestionBanks.Tags | Tags表 + 关联表 | 方案A | MVP阶段够用，避免过度设计 |
| **答题详情** | AttemptDetails表 | Attempts.Details(JSON) | 方案A | 需要统计分析，独立表更合适 |

---

## 4. AI Provider抽象设计

### 4.1 核心接口

```csharp
namespace AnswerMe.Domain.Interfaces;

public interface IAIProvider
{
    /// <summary>
    /// 生成题目
    /// </summary>
    Task<QuestionGenerationResult> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取Provider元数据
    /// </summary>
    ProviderMetadata GetMetadata();

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    Task<bool> ValidateConfigAsync(
        DataSourceConfig config,
        CancellationToken cancellationToken = default);
}

public record QuestionGenerationRequest
{
    public string Prompt { get; init; }
    public int Count { get; init; }
    public string Difficulty { get; init; }
    public string QuestionType { get; init; }
    public Dictionary<string, object> AdditionalParams { get; init; } = new();
}

public record QuestionGenerationResult
{
    public bool Success { get; init; }
    public List<GeneratedQuestion> Questions { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public int TokensUsed { get; init; }
}

public record GeneratedQuestion
{
    public string QuestionText { get; init; }
    public string QuestionType { get; init; }
    public List<string>? Options { get; init; }
    public string CorrectAnswer { get; init; }
    public string? Explanation { get; init; }
    public string Difficulty { get; init; }
}

public record ProviderMetadata
{
    public string Name { get; init; }
    public string Type { get; init; }
    public Dictionary<string, object> RequiredConfigFields { get; init; } = new();
    public int MaxQuestionsPerRequest { get; init; } = 10;
}
```

### 4.2 Provider工厂

```csharp
namespace AnswerMe.Infrastructure.AI;

public interface IAIProviderFactory
{
    IAIProvider CreateProvider(DataSource dataSource);
}

public class AIProviderFactory : IAIProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;

    public AIProviderFactory(IServiceProvider serviceProvider, HttpClient httpClient)
    {
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
    }

    public IAIProvider CreateProvider(DataSource dataSource)
    {
        return dataSource.Type switch
        {
            "openai" => new OpenAIProvider(_httpClient, dataSource.Config),
            "claude" => new ClaudeProvider(_httpClient, dataSource.Config),
            "qwen" => new QwenProvider(_httpClient, dataSource.Config),
            "custom_api" => new CustomApiProvider(_httpClient, dataSource.Config),
            _ => throw new NotSupportedException($"Provider type '{dataSource.Type}' is not supported")
        };
    }
}
```

### 4.3 MVP支持的Provider

#### 4.3.1 OpenAI Provider
```csharp
public class OpenAIProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIConfig _config;

    public OpenAIProvider(HttpClient httpClient, DataSourceConfig config)
    {
        _httpClient = httpClient;
        _config = config.ToObject<OpenAIConfig>();
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
    }

    public async Task<QuestionGenerationResult> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(request);
        var requestBody = new
        {
            model = _config.Model,
            messages = new[]
            {
                new { role = "system", content = "You are a question generator. Generate questions in JSON format." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            response_format = new { type = "json_object" }
        };

        var response = await _httpClient.PostAsJsonAsync("/chat/completions", requestBody, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // 解析响应并返回题目
        return ParseResponse(content);
    }

    public ProviderMetadata GetMetadata()
    {
        return new()
        {
            Name = "OpenAI",
            Type = "openai",
            RequiredConfigFields = new()
            {
                { "apiKey", "text" },
                { "baseUrl", "text" },
                { "model", "text" }
            },
            MaxQuestionsPerRequest = 10
        };
    }
}
```

#### 4.3.2 Custom API Provider
```csharp
public class CustomApiProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly CustomApiConfig _config;

    public CustomApiProvider(HttpClient httpClient, DataSourceConfig config)
    {
        _httpClient = httpClient;
        _config = config.ToObject<CustomApiConfig>();

        // 设置Headers
        foreach (var header in _config.Headers)
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
        }
    }

    public async Task<QuestionGenerationResult> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        // 构建请求体
        var requestBody = BuildRequestBody(request);

        // 发送请求
        var response = await _httpClient.SendAsync(CreateHttpRequest(requestBody), cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        // 使用JSONPath解析响应
        return ParseResponseWithJsonPath(content);
    }

    private QuestionGenerationResult ParseResponseWithJsonPath(string responseJson)
    {
        try
        {
            var json = JToken.Parse(responseJson);

            // 使用配置的JSONPath提取题目
            var questionsToken = json.SelectToken(_config.ResponseMapping.QuestionsPath);

            var questions = questionsToken.Select(q => new GeneratedQuestion
            {
                QuestionText = q.SelectToken(_config.ResponseMapping.QuestionText)?.ToString(),
                Options = q.SelectToken(_config.ResponseMapping.Options)?.ToObject<List<string>>(),
                CorrectAnswer = q.SelectToken(_config.ResponseMapping.CorrectAnswer)?.ToString(),
                // ...
            }).ToList();

            return new() { Success = true, Questions = questions };
        }
        catch (Exception ex)
        {
            return new()
            {
                Success = false,
                ErrorMessage = $"Failed to parse response: {ex.Message}"
            };
        }
    }
}
```

### 4.4 错误处理和重试

```csharp
public class ResilientAIProvider : IAIProvider
{
    private readonly IAIProvider _innerProvider;
    private readonly AsyncRetryPolicy _retryPolicy;

    public ResilientAIProvider(IAIProvider innerProvider)
    {
        _innerProvider = innerProvider;
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<AIProviderException>(ex => ex.IsTransient)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, delay, retryCount, context) =>
                {
                    Console.WriteLine($"Retry {retryCount} after {delay.TotalSeconds}s due to: {outcome.Exception?.Message}");
                });
    }

    public async Task<QuestionGenerationResult> GenerateQuestionsAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async () =>
        {
            return await _innerProvider.GenerateQuestionsAsync(request, cancellationToken);
        });
    }
}
```

---

## 5. 自定义API连接器设计

### 5.1 功能需求

用户可以配置任何HTTP API作为题目生成源：
- 配置请求：URL、Method、Headers、Body模板
- 解析响应：JSONPath提取题目数据
- 测试配置：保存前验证配置有效性

### 5.2 配置UI设计（MVP：简单表单）

```
┌────────────────────────────────────────────────────┐
│  自定义数据源配置                                    │
├────────────────────────────────────────────────────┤
│                                                     │
│  配置名称: [________________]                       │
│                                                     │
│  API端点:   [https://api.example.com/generate]     │
│                                                     │
│  HTTP方法:  [POST ▼] (GET/POST)                    │
│                                                     │
│  请求头:                                            │
│  ┌─────────────────────────────────────────────┐   │
│  │ Authorization: Bearer {token}               │   │
│  │ Content-Type: application/json              │   │
│  │ [+ 添加请求头]                              │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  请求模板 (JSON):                                   │
│  ┌─────────────────────────────────────────────┐   │
│  │ {                                           │   │
│  │   "prompt": "{prompt}",                     │   │
│  │   "count": {count},                         │   │
│  │   "difficulty": "{difficulty}"              │   │
│  │ }                                           │   │
│  └─────────────────────────────────────────────┘   │
│  可用变量: {prompt}, {count}, {difficulty}, {type}  │
│                                                     │
│  响应映射 (JSONPath):                               │
│  ┌─────────────────────────────────────────────┐   │
│  │ 题目数组路径: $.data.questions              │   │
│  │ 题目文本: $.text                            │   │
│  │ 选项数组: $.options                         │   │
│  │ 正确答案: $.answer                          │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  ┌──────────┐  ┌──────────┐                        │
│  │ 测试配置 │  │  保存    │                        │
│  └──────────┘  └──────────┘                        │
└────────────────────────────────────────────────────┘
```

### 5.3 配置验证逻辑

```csharp
public async Task<ValidationResult> ValidateConfigAsync(CustomApiConfig config)
{
    var errors = new List<string>();

    // 1. 验证URL格式
    if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out _))
    {
        errors.Add("Invalid API endpoint URL");
    }

    // 2. 验证JSONPath表达式
    try
    {
        var dummyJson = "{\"data\":{\"questions\":[{\"text\":\"test\",\"options\":[],\"answer\":\"A\"}]}}";
        JToken.Parse(dummyJson).SelectToken(config.ResponseMapping.QuestionsPath);
    }
    catch (Exception ex)
    {
        errors.Add($"Invalid JSONPath for questions: {ex.Message}");
    }

    // 3. 测试API调用（使用最小请求数据）
    try
    {
        var testRequest = new QuestionGenerationRequest
        {
            Prompt = "test",
            Count = 1,
            Difficulty = "easy"
        };

        var provider = new CustomApiProvider(_httpClient, config);
        var result = await provider.GenerateQuestionsAsync(testRequest);

        if (!result.Success)
        {
            errors.Add($"API test failed: {result.ErrorMessage}");
        }
    }
    catch (Exception ex)
    {
        errors.Add($"API test error: {ex.Message}");
    }

    return errors.Count == 0
        ? ValidationResult.Success()
        : ValidationResult.Failure(errors);
}
```

### 5.4 预设模板库

为了降低配置复杂度，提供常用API的预设模板：

```typescript
const TEMPLATES = {
  openai_compatible: {
    name: "OpenAI兼容API",
    config: {
      endpoint: "https://api.example.com/v1/chat/completions",
      method: "POST",
      headers: {
        "Authorization": "Bearer {apiKey}",
        "Content-Type": "application/json"
      },
      requestTemplate: {
        model: "{model}",
        messages: [
          { role: "system", content: "Generate questions in JSON format" },
          { role: "user", content: "{prompt}" }
        ]
      },
      responseMapping: {
        questionsPath: "$.choices[0].message.content",
        // 需要二次解析JSON字符串
      }
    }
  },
  simple_rest_api: {
    name: "简单REST API",
    config: {
      endpoint: "https://api.example.com/questions",
      method: "POST",
      headers: {},
      requestTemplate: {
        prompt: "{prompt}",
        count: {count}
      },
      responseMapping: {
        questionsPath: "$.questions",
        questionText: "$.question",
        options: "$.choices",
        correctAnswer: "$.correct"
      }
    }
  }
};
```

### 5.5 V2功能（未来考虑）

- **可视化流水线编辑器**：拖拽式配置预处理、后处理步骤
- **脚本支持**：允许用户编写JavaScript/C#脚本处理请求和响应
- **多步骤Pipeline**：支持先调用API A，再调用API B
- **Mock测试**：提供Mock响应用于离线测试

---

## 6. 部署方案

### 6.1 Docker Compose配置

```yaml
version: '3.8'

services:
  # 后端服务
  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: answerme-backend
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=answermedb;Username=answeruser;Password=${DB_PASSWORD}
      - JWT__Secret=${JWT_SECRET}
      - JWT__ExpiryDays=${JWT_EXPIRY_DAYS:-30}
      - Logging__LogLevel__Default=Information
    depends_on:
      db:
        condition: service_healthy
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - answerme-network

  # 前端服务
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: ${VITE_API_BASE_URL:-http://localhost:5000}
    container_name: answerme-frontend
    ports:
      - "3000:80"
    environment:
      - VITE_API_BASE_URL=${VITE_API_BASE_URL:-http://localhost:5000}
    depends_on:
      - backend
    restart: unless-stopped
    networks:
      - answerme-network

  # PostgreSQL数据库
  db:
    image: postgres:16-alpine
    container_name: answerme-db
    volumes:
      - pgdata:/var/lib/postgresql/data
    environment:
      - POSTGRES_DB=answermedb
      - POSTGRES_USER=answeruser
      - POSTGRES_PASSWORD=${DB_PASSWORD}
      - PGDATA=/var/lib/postgresql/data/pgdata
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U answeruser -d answermedb"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped
    networks:
      - answerme-network

  # Nginx反向代理（可选）
  nginx:
    image: nginx:alpine
    container_name: answerme-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - frontend
      - backend
    restart: unless-stopped
    networks:
      - answerme-network
    profiles:
      - production

volumes:
  pgdata:
    driver: local

networks:
  answerme-network:
    driver: bridge
```

### 6.2 环境变量配置（.env.example）

```bash
# ===========================================
# AnswerMe 环境变量配置示例
# 复制此文件为 .env 并修改相应值
# ===========================================

# -----------------------
# 数据库配置
# -----------------------
DB_PASSWORD=your_secure_password_here
# 生成强密码命令: openssl rand -base64 32

# -----------------------
# JWT认证配置
# -----------------------
JWT_SECRET=your_jwt_secret_key_here
# 生成密钥命令: openssl rand -base64 64
JWT_EXPIRY_DAYS=30

# -----------------------
# 应用配置
# -----------------------
ASPNETCORE_ENVIRONMENT=Production
VITE_API_BASE_URL=http://localhost:5000

# -----------------------
# 日志配置
# -----------------------
LOG_LEVEL=Information
# 可选: Debug, Information, Warning, Error

# -----------------------
# 限流配置（可选）
# -----------------------
RATE_LIMIT_ENABLED=true
RATE_LIMIT_PER_MINUTE=60

# -----------------------
# 备份配置（可选）
# -----------------------
BACKUP_ENABLED=false
BACKUP_RETENTION_DAYS=7
BACKUP_CRON="0 2 * * *"  # 每天凌晨2点
```

### 6.3 健康检查端点设计

```csharp
// Health/HealthEndpoints.cs

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow
    });
});

app.MapGet("/health/ready", async (
    IHealthCheckService healthCheckService) =>
{
    var report = await healthCheckService.CheckHealthAsync();
    return report.Status == HealthStatus.Healthy
        ? Results.Ok(report)
        : Results.StatusCode(503);
});

app.MapGet("/health/live", () =>
{
    return Results.Ok(new
    {
        status = "alive",
        timestamp = DateTime.UtcNow
    });
});
```

健康检查配置：
```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        name: "postgresql",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "db", "ready" })
    .AddCheck<ExternalAPIHealthCheck>(
        "external-apis",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "external", "ready" });
```

### 6.4 启动脚本

```bash
#!/bin/bash
# start.sh

set -e

echo "🚀 Starting AnswerMe..."

# 检查.env文件
if [ ! -f .env ]; then
    echo "❌ .env file not found. Copy .env.example to .env and configure it."
    exit 1
fi

# 构建并启动服务
docker-compose up -d --build

echo "⏳ Waiting for services to be ready..."
sleep 10

# 检查服务状态
if curl -f http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ Backend is healthy"
else
    echo "❌ Backend health check failed"
    docker-compose logs backend
    exit 1
fi

if curl -f http://localhost:3000 > /dev/null 2>&1; then
    echo "✅ Frontend is healthy"
else
    echo "❌ Frontend health check failed"
    docker-compose logs frontend
    exit 1
fi

echo "🎉 AnswerMe is now running!"
echo "   Frontend: http://localhost:3000"
echo "   Backend:  http://localhost:5000"
echo "   Health:   http://localhost:5000/health"
```

### 6.5 数据备份脚本

```bash
#!/bin/bash
# backup.sh

set -e

BACKUP_DIR="./backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/answermedb_${TIMESTAMP}.sql.gz"

mkdir -p ${BACKUP_DIR}

echo "📦 Backing up database..."

docker-compose exec -T db pg_dump -U answeruser answermedb | gzip > ${BACKUP_FILE}

echo "✅ Backup saved to: ${BACKUP_FILE}"

# 清理旧备份（保留最近N个）
find ${BACKUP_DIR} -name "answermedb_*.sql.gz" -mtime +${BACKUP_RETENTION_DAYS:-7} -delete

echo "🧹 Old backups cleaned up"
```

---

## 7. 风险点与缓解措施

### 7.1 技术风险

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| **AI调用不稳定** | 题目生成失败 | 高 | 1. 重试机制（3次，指数退避）<br>2. 超时控制（30s）<br>3. 友好错误提示 |
| **自定义API配置复杂** | 用户无法成功配置 | 中 | 1. 配置测试按钮<br>2. 预设模板库<br>3. 详细错误信息<br>4. 示例文档 |
| **数据库性能瓶颈** | 查询慢、响应延迟 | 中 | 1. 索引优化（UserId, BankId）<br>2. 分页查询<br>3. 未来：Redis缓存 |
| **并发答题冲突** | 数据不一致 | 低 | 1. 乐观锁（Version字段）<br>2. 幂等性设计<br>3. 事务隔离 |

### 7.2 安全风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| **API密钥泄露** | 用户额度被盗用 | 1. 数据库加密（AES-256）<br>2. 不记录日志<br>3. HTTPS强制<br>4. 密钥脱敏显示 |
| **恶意请求刷接口** | 用户额度耗尽 | 1. Rate Limiting（60次/分钟）<br>2. 请求审计日志<br>3. 异常告警 |
| **XSS注入** | 用户会话劫持 | 1. 输入验证（题目内容）<br>2. 输出转义（Vue自动）<br>3. CSP策略 |
| **SQL注入** | 数据泄露 | 1. EF Core参数化查询<br>2. 禁止拼接SQL<br>3. 最小权限数据库用户 |

### 7.3 产品风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| **AI生成质量差** | 用户流失 | 1. Prompt工程优化<br>2. 题目编辑功能<br>3. 用户反馈机制<br>4. 未来：质量评分 |
| **依赖第三方API变更** | 功能不可用 | 1. 版本锁定<br>2. 适配器模式<br>3. 及时更新跟进 |

### 7.4 运维风险

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| **数据丢失** | 用户数据无法恢复 | 1. 自动备份脚本<br>2. 备份文档说明<br>3. 恢复流程测试 |
| **升级失败** | 系统不可用 | 1. 数据库迁移脚本<br>2. 向下兼容测试<br>3. 回滚方案（Docker版本标签） |
| **资源耗尽** | 服务崩溃 | 1. 容器资源限制（memory, CPU）<br>2. 日志轮转<br>3. 监控告警 |

---

## 8. 技术栈清单

### 8.1 后端

| 组件 | 技术 | 版本 | 用途 |
|------|------|------|------|
| 框架 | ASP.NET Core | 8.0 | Web API框架 |
| ORM | Entity Framework Core | 8.0 | 数据访问 |
| 数据库 | PostgreSQL | 16 | 数据存储 |
| 认证 | JWT (System.IdentityModel.Tokens.Jwt) | latest | Token认证 |
| HTTP | HttpClient (Microsoft.Extensions.Http) | latest | 外部API调用 |
| 重试策略 | Polly | latest | 弹性重试 |
| JSON处理 | System.Text.Json | latest | JSON序列化 |
| JSONPath | JsonPath.Net | latest | 响应解析 |
| 日志 | Serilog | latest | 结构化日志 |
| 验证 | FluentValidation | latest | 请求验证 |
| 测试 | xUnit + Moq | latest | 单元测试 |

### 8.2 前端

| 组件 | 技术 | 版本 | 用途 |
|------|------|------|------|
| 框架 | Vue | 3.4+ | 前端框架 |
| 语言 | TypeScript | 5.0+ | 类型安全 |
| 构建 | Vite | 5.0+ | 构建工具 |
| 状态管理 | Pinia | 2.0+ | 全局状态 |
| 路由 | Vue Router | 4.0+ | 页面路由 |
| UI库 | Element Plus | 2.0+ | UI组件 |
| HTTP客户端 | Axios | latest | API调用 |
| 表单验证 | VeeValidate | latest | 表单验证 |
| 代码规范 | ESLint + Prettier | latest | 代码质量 |
| 测试 | Vitest | latest | 单元测试 |

### 8.3 DevOps

| 组件 | 技术 | 版本 | 用途 |
|------|------|------|------|
| 容器化 | Docker | 24+ | 容器镜像 |
| 编排 | Docker Compose | 2.20+ | 本地部署 |
| 反向代理 | Nginx | 1.25+ | 生产环境 |
| CI/CD | GitHub Actions | latest | 自动化部署 |
| 监控 | Prometheus + Grafana | latest | 监控告警（未来） |

---

## 9. 开发路线图

### Phase 1: MVP（4-6周）
- [ ] 用户认证（注册、登录、JWT）
- [ ] 数据源配置（OpenAI + 1个国内模型）
- [ ] 题目生成（单次生成、保存到题库）
- [ ] 答题功能（顺序答题、计分）
- [ ] Docker部署

### Phase 2: 自定义API（2-3周）
- [ ] 自定义API配置表单
- [ ] JSONPath解析器
- [ ] 配置验证和测试
- [ ] 预设模板库

### Phase 3: 增强功能（3-4周）
- [ ] 题目编辑
- [ ] 批量导入导出
- [ ] 答题统计和图表
- [ ] 标签和筛选

### Phase 4: 生产优化（2-3周）
- [ ] 性能优化（缓存、索引）
- [ ] 监控和日志
- [ ] 自动化备份
- [ ] 文档和示例

---

## 10. 后续优化方向

1. **架构优化**：
   - 引入消息队列（RabbitMQ）处理异步任务
   - 添加缓存层（Redis）提升性能
   - 考虑微服务拆分（题目生成、答题、统计）

2. **功能扩展**：
   - 题目推荐算法
   - 协作编辑（多人维护题库）
   - 知识图谱（题目关联）
   - AI质量评估

3. **用户体验**：
   - 移动端适配（PWA）
   - 离线答题支持
   - 导出为PDF/Word
   - 国际化（i18n）

4. **运维优化**：
   - Kubernetes部署支持
   - 自动扩缩容
   - A/B测试框架
   - 用户行为分析

---

## 11. 总结

**核心架构决策**：
- ✅ 单体应用：适合MVP，降低复杂度
- ✅ PostgreSQL：关系型+JSONB，平衡灵活性和一致性
- ✅ AI Provider抽象：支持多种数据源，易于扩展
- ✅ Docker Compose：一键部署，自托管友好
- ✅ 自定义API连接器：简单表单配置，JSONPath解析

**关键风险控制**：
- 🔒 API密钥加密存储
- ⚡ 重试机制和错误处理
- 🛡️ Rate Limiting防止滥用
- 📦 自动备份避免数据丢失

**下一步行动**：
1. 初始化.NET项目结构
2. 设计数据库迁移脚本
3. 实现OpenAI Provider
4. 搭建前端基础框架
5. 完成Docker Compose配置
