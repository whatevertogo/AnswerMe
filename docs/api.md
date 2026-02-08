# API 文档

AI Question Bank RESTful API完整文档。

## 目录

- [基础信息](#基础信息)
- [认证](#认证)
- [用户管理](#用户管理)
- [题库管理](#题库管理)
- [题目管理](#题目管理)
- [AI生成](#ai生成)
- [答题记录](#答题记录)
- [数据导入导出](#数据导入导出)
- [错误处理](#错误处理)

## 基础信息

### Base URL

```
开发环境: http://localhost:5000
生产环境: https://api.your-domain.com
```

### 响应格式

所有响应均为JSON格式:

```json
{
  "success": true,
  "data": {},
  "message": "操作成功",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 错误响应

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "请求参数验证失败",
    "details": ["Email格式不正确"]
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

### 分页响应

使用游标分页:

```json
{
  "items": [],
  "nextCursor": "eyJpZCI6IjEyMzQ1Njc4LTkwYWItY2RlZi0xMjM0LTU2Nzg5MGFiY2RlZiJ9",
  "hasMore": true
}
```

### 速率限制

- 默认: 60请求/分钟
- AI生成: 10请求/小时

响应头:

```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1704067200
```

## 认证

### 注册

```http
POST /api/auth/register
Content-Type: application/json
```

**请求体**:

```json
{
  "email": "user@example.com",
  "password": "Secure@Password123",
  "confirmPassword": "Secure@Password123"
}
```

**响应**: `201 Created`

```json
{
  "success": true,
  "data": {
    "user": {
      "id": "12345678-90ab-cdef-1234-567890abcdef",
      "email": "user@example.com",
      "createdAt": "2024-01-01T00:00:00Z"
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

### 登录

```http
POST /api/auth/login
Content-Type: application/json
```

**请求体**:

```json
{
  "email": "user@example.com",
  "password": "Secure@Password123"
}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "user": {
      "id": "12345678-90ab-cdef-1234-567890abcdef",
      "email": "user@example.com"
    },
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

### 使用Token

在请求头中包含Token:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 获取当前用户信息

```http
GET /api/auth/me
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "12345678-90ab-cdef-1234-567890abcdef",
    "email": "user@example.com",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

## 用户管理

### 更新用户信息

```http
PUT /api/users/me
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "email": "newemail@example.com"
}
```

**响应**: `200 OK`

### 修改密码

```http
POST /api/users/me/change-password
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "currentPassword": "Old@Password123",
  "newPassword": "New@Password456"
}
```

**响应**: `200 OK`

## AI配置管理

### 添加AI配置

```http
POST /api/ai/configs
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "provider": "openai",
  "apiKey": "sk-proj-...",
  "model": "gpt-4o-mini",
  "apiBase": "https://api.openai.com/v1"
}
```

**响应**: `201 Created`

```json
{
  "success": true,
  "data": {
    "id": "config-id",
    "provider": "openai",
    "model": "gpt-4o-mini",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

**注意**: API密钥会被加密存储,响应中不返回明文密钥。

### 验证AI配置

```http
POST /api/ai/configs/{configId}/validate
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "valid": true,
    "message": "API密钥有效"
  }
}
```

### 获取AI配置列表

```http
GET /api/ai/configs
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": [
    {
      "id": "config-id",
      "provider": "openai",
      "model": "gpt-4o-mini",
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### 删除AI配置

```http
DELETE /api/ai/configs/{configId}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

## 题库管理

### 创建题库

```http
POST /api/questionbanks
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "name": "Python基础知识",
  "description": "Python编程基础题目",
  "tags": ["python", "编程", "基础"]
}
```

**响应**: `201 Created`

```json
{
  "success": true,
  "data": {
    "id": "bank-id",
    "name": "Python基础知识",
    "description": "Python编程基础题目",
    "tags": ["python", "编程", "基础"],
    "questionCount": 0,
    "version": 1,
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### 获取题库列表

```http
GET /api/questionbanks?cursor={cursor}&limit=20&search=python
Authorization: Bearer {token}
```

**查询参数**:
- `cursor`: 游标(可选)
- `limit`: 每页数量(默认20,最大100)
- `search`: 搜索关键词(可选)

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "bank-id",
        "name": "Python基础知识",
        "description": "Python编程基础题目",
        "questionCount": 50,
        "createdAt": "2024-01-01T00:00:00Z"
      }
    ],
    "nextCursor": "eyJpZCI6IjEyMzQ1Njc4LTkwYWItY2RlZi0xMjM0LTU2Nzg5MGFiY2RlZiJ9",
    "hasMore": true
  }
}
```

### 获取题库详情

```http
GET /api/questionbanks/{bankId}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "bank-id",
    "name": "Python基础知识",
    "description": "Python编程基础题目",
    "tags": ["python", "编程", "基础"],
    "questionCount": 50,
    "version": 1,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

### 更新题库

```http
PUT /api/questionbanks/{bankId}
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "name": "Python进阶知识",
  "description": "Python高级编程题目",
  "tags": ["python", "编程", "进阶"],
  "version": 1
}
```

**注意**: `version`字段用于乐观锁,更新时必须提供当前版本号。

**响应**: `200 OK`

### 删除题库

```http
DELETE /api/questionbanks/{bankId}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

## 题目管理

### 创建题目

```http
POST /api/questionbanks/{bankId}/questions
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "type": "single_choice",
  "content": "Python中哪个关键字用于定义函数?",
  "options": ["func", "def", "function", "define"],
  "correctAnswer": "def",
  "explanation": "def是Python中定义函数的关键字",
  "difficulty": "easy",
  "tags": ["python", "函数"]
}
```

**题目类型**:
- `single_choice`: 单选题
- `multiple_choice`: 多选题
- `true_false`: 判断题
- `fill_blank`: 填空题
- `short_answer`: 简答题

**难度级别**:
- `easy`: 简单
- `medium`: 中等
- `hard`: 困难

**响应**: `201 Created`

### 获取题目列表

```http
GET /api/questionbanks/{bankId}/questions?cursor={cursor}&limit=20&difficulty=easy&type=single_choice
Authorization: Bearer {token}
```

**查询参数**:
- `cursor`: 游标(可选)
- `limit`: 每页数量(默认20,最大100)
- `difficulty`: 难度筛选(可选)
- `type`: 题型筛选(可选)
- `tags`: 标签筛选(可选)

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "question-id",
        "type": "single_choice",
        "content": "Python中哪个关键字用于定义函数?",
        "options": ["func", "def", "function", "define"],
        "difficulty": "easy",
        "tags": ["python", "函数"]
      }
    ],
    "nextCursor": "...",
    "hasMore": true
  }
}
```

### 获取题目详情

```http
GET /api/questionbanks/{bankId}/questions/{questionId}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "id": "question-id",
    "type": "single_choice",
    "content": "Python中哪个关键字用于定义函数?",
    "options": ["func", "def", "function", "define"],
    "correctAnswer": "def",
    "explanation": "def是Python中定义函数的关键字",
    "difficulty": "easy",
    "tags": ["python", "函数"],
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

### 更新题目

```http
PUT /api/questionbanks/{bankId}/questions/{questionId}
Authorization: Bearer {token}
Content-Type: application/json
```

**响应**: `200 OK`

### 删除题目

```http
DELETE /api/questionbanks/{bankId}/questions/{questionId}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

## AI生成

### 生成题目(同步)

```http
POST /api/ai/generate
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "bankId": "bank-id",
  "configId": "ai-config-id",
  "topic": "Python基础知识",
  "count": 10,
  "difficulty": "easy",
  "types": ["single_choice", "true_false"],
  "language": "zh-CN"
}
```

**响应**: `200 OK` (同步)

```json
{
  "success": true,
  "data": {
    "questions": [
      {
        "id": "generated-question-id",
        "type": "single_choice",
        "content": "Python是什么?",
        "options": [...],
        "correctAnswer": "...",
        "explanation": "...",
        "difficulty": "easy"
      }
    ],
    "totalCount": 10,
    "successCount": 10,
    "failedCount": 0
  }
}
```

### 生成题目(异步)

对于大量题目(>20题),使用异步生成:

**请求**: 同上,但 `count > 20`

**响应**: `202 Accepted`

```json
{
  "success": true,
  "data": {
    "jobId": "job-id",
    "status": "processing",
    "message": "AI生成任务已创建"
  }
}
```

### 查询生成进度

```http
GET /api/ai/generate/{jobId}/status
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "jobId": "job-id",
    "status": "processing",
    "progress": 5,
    "total": 50,
    "completed": 5,
    "failed": 0,
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

**状态**:
- `pending`: 等待中
- `processing`: 处理中
- `completed`: 已完成
- `failed`: 失败
- `partial_success`: 部分成功

## 答题记录

### 提交答案

```http
POST /api/questionbanks/{bankId}/questions/{questionId}/answer
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "answer": "def",
  "timeSpent": 30
}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "correct": true,
    "correctAnswer": "def",
    "explanation": "def是Python中定义函数的关键字",
    "attempts": 1
  }
}
```

### 获取答题记录

```http
GET /api/questionbanks/{bankId}/attempts?cursor={cursor}&limit=20
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": "attempt-id",
        "questionId": "question-id",
        "question": {
          "content": "Python中哪个关键字用于定义函数?"
        },
        "userAnswer": "def",
        "correct": true,
        "timeSpent": 30,
        "answeredAt": "2024-01-01T00:00:00Z"
      }
    ],
    "nextCursor": "...",
    "hasMore": false
  }
}
```

### 获取统计信息

```http
GET /api/questionbanks/{bankId}/statistics
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "success": true,
  "data": {
    "totalQuestions": 50,
    "answeredQuestions": 30,
    "correctCount": 25,
    "incorrectCount": 5,
    "accuracy": 0.833,
    "averageTimeSpent": 45.5,
    "difficultyDistribution": {
      "easy": { "total": 20, "answered": 15, "correct": 14 },
      "medium": { "total": 20, "answered": 10, "correct": 8 },
      "hard": { "total": 10, "answered": 5, "correct": 3 }
    }
  }
}
```

## 数据导入导出

### 导出题库

```http
GET /api/questionbanks/{bankId}/export
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "version": "1.0",
  "exportedAt": "2024-01-01T00:00:00Z",
  "bank": {
    "id": "bank-id",
    "name": "Python基础知识",
    "description": "Python编程基础题目",
    "tags": ["python", "编程", "基础"]
  },
  "questions": [
    {
      "type": "single_choice",
      "content": "Python中哪个关键字用于定义函数?",
      "options": ["func", "def", "function", "define"],
      "correctAnswer": "def",
      "explanation": "def是Python中定义函数的关键字",
      "difficulty": "easy",
      "tags": ["python", "函数"]
    }
  ]
}
```

### 导出所有数据

```http
GET /api/export/all
Authorization: Bearer {token}
```

**响应**: `200 OK`

包含所有题库、题目、答题记录的完整数据。

### 导入数据

```http
POST /api/import
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**: 导出的JSON格式

**响应**: `201 Created`

```json
{
  "success": true,
  "data": {
    "importedBanks": 5,
    "importedQuestions": 250,
    "conflicts": [
      {
        "type": "duplicate_name",
        "entity": "questionbank",
        "name": "Python基础知识",
        "action": "renamed"
      }
    ]
  }
}
```

## 健康检查

### 健康检查

```http
GET /health
```

**响应**: `200 OK`

```json
{
  "status": "healthy",
  "timestamp": "2024-01-01T00:00:00Z",
  "checks": {
    "database": "healthy",
    "memory": "healthy",
    "disk": "healthy"
  }
}
```

## 错误处理

### 错误码

| 错误码 | HTTP状态 | 说明 |
|--------|----------|------|
| `VALIDATION_ERROR` | 400 | 请求参数验证失败 |
| `UNAUTHORIZED` | 401 | 未认证 |
| `FORBIDDEN` | 403 | 权限不足 |
| `NOT_FOUND` | 404 | 资源不存在 |
| `CONFLICT` | 409 | 资源冲突(如版本不匹配) |
| `RATE_LIMIT_EXCEEDED` | 429 | 超过速率限制 |
| `INTERNAL_ERROR` | 500 | 服务器内部错误 |
| `AI_SERVICE_ERROR` | 502 | AI服务不可用 |
| `AI_QUOTA_EXCEEDED` | 503 | AI配额已用完 |

### 错误响应示例

**验证错误**:

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "请求参数验证失败",
    "details": [
      "Email格式不正确",
      "密码长度至少8个字符"
    ]
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

**认证错误**:

```json
{
  "success": false,
  "error": {
    "code": "UNAUTHORIZED",
    "message": "Token无效或已过期"
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

**冲突错误**:

```json
{
  "success": false,
  "error": {
    "code": "CONFLICT",
    "message": "题库已被其他用户修改",
    "details": {
      "currentVersion": 2,
      "providedVersion": 1
    }
  },
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## SDK和客户端

### JavaScript/TypeScript

```bash
npm install @ai-questionbank/sdk
```

```typescript
import { QuestionBankClient } from '@ai-questionbank/sdk';

const client = new QuestionBankClient({
  baseURL: 'http://localhost:5000',
  token: 'your-jwt-token'
});

// 创建题库
const bank = await client.questionBanks.create({
  name: 'Python基础知识',
  description: 'Python编程基础题目'
});

// 生成题目
const questions = await client.ai.generate({
  bankId: bank.id,
  configId: 'config-id',
  topic: 'Python基础知识',
  count: 10,
  difficulty: 'easy'
});
```

### Python

```bash
pip install ai-questionbank
```

```python
from ai_questionbank import QuestionBankClient

client = QuestionBankClient(
    base_url='http://localhost:5000',
    token='your-jwt-token'
)

# 创建题库
bank = client.question_banks.create(
    name='Python基础知识',
    description='Python编程基础题目'
)

# 生成题目
questions = client.ai.generate(
    bank_id=bank.id,
    config_id='config-id',
    topic='Python基础知识',
    count=10,
    difficulty='easy'
)
```

## 更新日志

### v0.1.0-alpha (2024-01-01)

- ✨ 初始版本
- ✨ 用户认证系统
- ✨ 题库管理
- ✨ AI题目生成
- ✨ 答题记录
- ✨ 数据导入导出

## 反馈和支持

- 📖 [完整文档](../README.md)
- 💬 [Discussions](https://github.com/your-username/ai-questionbank/discussions)
- 🐛 [Bug报告](https://github.com/your-username/ai-questionbank/issues)
