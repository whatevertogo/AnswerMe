# API 文档

AI Question Bank RESTful API完整文档。

## 目录

- [基础信息](#基础信息)
- [认证](#认证)
- [题库管理](#题库管理)
- [题目管理](#题目管理)
- [AI生成](#ai生成)
- [答题记录](#答题记录)
- [数据源管理](#数据源管理)
- [错误处理](#错误处理)

## 基础信息

### Base URL

```
开发环境: http://localhost:5000
生产环境: https://api.your-domain.com
```

### 响应格式

所有响应均为JSON格式，直接返回数据对象：

```json
{
  "id": "12345678-90ab-cdef-1234-567890abcdef",
  "email": "user@example.com",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

### 错误响应

```json
{
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
  "id": "12345678-90ab-cdef-1234-567890abcdef",
  "email": "user@example.com",
  "createdAt": "2024-01-01T00:00:00Z"
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
  "user": {
    "id": "12345678-90ab-cdef-1234-567890abcdef",
    "email": "user@example.com"
  },
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 本地模式登录

```http
POST /api/auth/local-login
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
  "id": "12345678-90ab-cdef-1234-567890abcdef",
  "email": "user@example.com",
  "createdAt": "2024-01-01T00:00:00Z"
}
```

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
  "id": "bank-id",
  "name": "Python基础知识",
  "description": "Python编程基础题目",
  "tags": ["python", "编程", "基础"],
  "questionCount": 0,
  "version": 1,
  "createdAt": "2024-01-01T00:00:00Z"
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
```

### 获取题库详情

```http
GET /api/questionbanks/{id}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "id": "bank-id",
  "name": "Python基础知识",
  "description": "Python编程基础题目",
  "tags": ["python", "编程", "基础"],
  "questionCount": 50,
  "version": 1,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### 更新题库

```http
PUT /api/questionbanks/{id}
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
DELETE /api/questionbanks/{id}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

### 导出题库

```http
GET /api/questionbanks/{id}/export
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

## 题目管理

### 题目数据结构规范

**重要提示**：API 使用新字段作为唯一对外标准，旧字段（`type`、`options`、`correctAnswer`）仅用于向后兼容的历史数据读取。

**新字段标准**：
- `questionTypeEnum`: 题型枚举（SingleChoice/MultipleChoice/TrueFalse/FillBlank/ShortAnswer）
- `data`: QuestionData 对象，包含题型特定数据
  - `ChoiceQuestionData`: 选择题数据（单选/多选）
    - `type`: "ChoiceQuestionData"
    - `options`: 选项列表
    - `correctAnswers`: 正确答案列表
    - `explanation`: 解析
    - `difficulty`: 难度
  - `BooleanQuestionData`: 判断题数据
    - `type`: "BooleanQuestionData"
    - `correctAnswer`: 正确答案（布尔值）
  - `FillBlankQuestionData`: 填空题数据
    - `type`: "FillBlankQuestionData"
    - `acceptableAnswers`: 可接受答案列表
  - `ShortAnswerQuestionData`: 简答题数据
    - `type`: "ShortAnswerQuestionData"
    - `referenceAnswer`: 参考答案

### 获取题目列表

```http
GET /api/questions?cursor={cursor}&limit=20&bankId={bankId}&difficulty=easy&questionTypeEnum=SingleChoice
Authorization: Bearer {token}
```

**查询参数**:
- `cursor`: 游标(可选)
- `limit`: 每页数量(默认20,最大100)
- `bankId`: 题库ID(可选)
- `difficulty`: 难度筛选(可选)
- `questionTypeEnum`: 题型筛选枚举(可选，推荐使用)

**响应**: `200 OK`

```json
{
  "data": [
    {
      "id": 1,
      "questionBankId": 1,
      "questionBankName": "Python基础知识",
      "questionText": "Python中哪个关键字用于定义函数?",
      "questionTypeEnum": "SingleChoice",
      "data": {
        "type": "ChoiceQuestionData",
        "options": ["func", "def", "function", "define"],
        "correctAnswers": ["def"],
        "explanation": "def是Python中定义函数的关键字",
        "difficulty": "easy"
      },
      "explanation": "def是Python中定义函数的关键字",
      "difficulty": "easy",
      "orderIndex": 1,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  ],
  "hasMore": true,
  "nextCursor": 2,
  "totalCount": 50
}
```

### 搜索题目

```http
GET /api/questions/search?q=python&bankId={bankId}
Authorization: Bearer {token}
```

**查询参数**:
- `q`: 搜索关键词
- `bankId`: 题库ID(可选)

**响应**: `200 OK`

### 获取题目详情

```http
GET /api/questions/{id}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "id": 1,
  "questionBankId": 1,
  "questionBankName": "Python基础知识",
  "questionText": "Python中哪个关键字用于定义函数?",
  "questionTypeEnum": "SingleChoice",
  "data": {
    "type": "ChoiceQuestionData",
    "options": ["func", "def", "function", "define"],
    "correctAnswers": ["def"],
    "explanation": "def是Python中定义函数的关键字",
    "difficulty": "easy"
  },
  "explanation": "def是Python中定义函数的关键字",
  "difficulty": "easy",
  "orderIndex": 1,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### 创建题目

```http
POST /api/questions
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体（新字段标准）**：

```json
{
  "questionBankId": 1,
  "questionText": "Python中哪个关键字用于定义函数?",
  "questionTypeEnum": "SingleChoice",
  "data": {
    "type": "ChoiceQuestionData",
    "options": ["func", "def", "function", "define"],
    "correctAnswers": ["def"],
    "explanation": "def是Python中定义函数的关键字",
    "difficulty": "easy"
  },
  "explanation": "def是Python中定义函数的关键字",
  "difficulty": "easy",
  "orderIndex": 1
}
```

**题目类型枚举**:
- `SingleChoice`: 单选题
- `MultipleChoice`: 多选题
- `TrueFalse`: 判断题
- `FillBlank`: 填空题
- `ShortAnswer`: 简答题

**难度级别**:
- `easy`: 简单
- `medium`: 中等
- `hard`: 困难

**响应**: `201 Created`

```json
{
  "id": 1,
  "questionBankId": 1,
  "questionBankName": "Python基础知识",
  "questionText": "Python中哪个关键字用于定义函数?",
  "questionTypeEnum": "SingleChoice",
  "data": {
    "type": "ChoiceQuestionData",
    "options": ["func", "def", "function", "define"],
    "correctAnswers": ["def"],
    "explanation": "def是Python中定义函数的关键字",
    "difficulty": "easy"
  },
  "explanation": "def是Python中定义函数的关键字",
  "difficulty": "easy",
  "orderIndex": 1,
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### 更新题目

```http
PUT /api/questions/{id}
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体（新字段标准）**：

```json
{
  "questionText": "Python中哪个关键字用于定义函数?",
  "questionTypeEnum": "SingleChoice",
  "data": {
    "type": "ChoiceQuestionData",
    "options": ["func", "def", "function", "define"],
    "correctAnswers": ["def"],
    "explanation": "def是Python中定义函数的关键字",
    "difficulty": "easy"
  },
  "explanation": "def是Python中定义函数的关键字",
  "difficulty": "easy"
}
```

**响应**: `200 OK`

### 删除题目

```http
DELETE /api/questions/{id}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

### 批量创建题目

```http
POST /api/questions/batch
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体（新字段标准）**：

```json
[
  {
    "questionBankId": 1,
    "questionText": "Python中哪个关键字用于定义函数?",
    "questionTypeEnum": "SingleChoice",
    "data": {
      "type": "ChoiceQuestionData",
      "options": ["func", "def", "function", "define"],
      "correctAnswers": ["def"],
      "explanation": "def是Python中定义函数的关键字",
      "difficulty": "easy"
    },
    "difficulty": "easy",
    "orderIndex": 1
  }
]
```

**响应**: `201 Created`

### 批量删除题目

```http
POST /api/questions/batch-delete
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "ids": [1, 2, 3]
}
```

**响应**: `200 OK`

```json
{
  "successCount": 2,
  "notFoundCount": 1
}
```

## AI生成

### 生成题目(同步)

```http
POST /api/aigeneration/generate
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "bankId": "bank-id",
  "datasourceId": "datasource-id",
  "topic": "Python基础知识",
  "count": 10,
  "difficulty": "easy",
  "types": ["single_choice", "true_false"]
}
```

**注意**: 同步生成适用于题目数量≤20的情况

**响应**: `200 OK`

```json
{
  "questions": [
    {
      "id": "generated-question-id",
      "type": "single_choice",
      "content": "Python是什么?",
      "data": {
        "options": [...],
        "correctAnswer": "..."
      },
      "explanation": "...",
      "difficulty": "easy"
    }
  ],
  "totalCount": 10,
  "successCount": 10,
  "failedCount": 0
}
```

### 生成题目(异步)

```http
POST /api/aigeneration/generate-async
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "bankId": "bank-id",
  "datasourceId": "datasource-id",
  "topic": "Python基础知识",
  "count": 50,
  "difficulty": "easy",
  "types": ["single_choice", "true_false"]
}
```

**注意**: 异步生成适用于题目数量>20的情况

**响应**: `202 Accepted`

```json
{
  "taskId": "task-id",
  "status": "processing",
  "message": "AI生成任务已创建"
}
```

### 查询生成进度

```http
GET /api/aigeneration/progress/{taskId}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "taskId": "task-id",
  "status": "processing",
  "progress": 5,
  "total": 50,
  "completed": 5,
  "failed": 0,
  "createdAt": "2024-01-01T00:00:00Z"
}
```

**状态**:
- `pending`: 等待中
- `processing`: 处理中
- `completed`: 已完成
- `failed`: 失败
- `partial_success`: 部分成功

## 答题记录

### 开始答题

```http
POST /api/attempts/start
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "bankId": "bank-id",
  "questionIds": ["question-id-1", "question-id-2"]
}
```

**响应**: `200 OK`

### 提交答案

```http
POST /api/attempts/submit-answer
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "attemptId": "attempt-id",
  "questionId": "question-id",
  "answer": "def",
  "timeSpent": 30
}
```

**响应**: `200 OK`

```json
{
  "correct": true,
  "correctAnswer": "def",
  "explanation": "def是Python中定义函数的关键字",
  "attempts": 1
}
```

### 完成答题

```http
POST /api/attempts/complete
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "attemptId": "attempt-id"
}
```

**响应**: `200 OK`

### 获取答题记录详情

```http
GET /api/attempts/{id}
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "id": "attempt-id",
  "bankId": "bank-id",
  "userId": "user-id",
  "status": "completed",
  "startedAt": "2024-01-01T00:00:00Z",
  "completedAt": "2024-01-01T00:30:00Z"
}
```

### 获取答题详情列表

```http
GET /api/attempts/{id}/details
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "items": [
    {
      "id": "detail-id",
      "attemptId": "attempt-id",
      "questionId": "question-id",
      "answer": "def",
      "correct": true,
      "timeSpent": 30,
      "answeredAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### 获取答题统计

```http
GET /api/attempts/statistics?bankId={bankId}
Authorization: Bearer {token}
```

**查询参数**:
- `bankId`: 题库ID(可选)

**响应**: `200 OK`

```json
{
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
```

## 数据源管理

### 获取数据源列表

```http
GET /api/datasource
Authorization: Bearer {token}
```

**响应**: `200 OK`

```json
{
  "items": [
    {
      "id": "datasource-id",
      "name": "OpenAI",
      "type": "openai",
      "isDefault": true,
      "createdAt": "2024-01-01T00:00:00Z"
    }
  ]
}
```

### 创建数据源

```http
POST /api/datasource
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "name": "OpenAI",
  "type": "openai",
  "config": {
    "apiKey": "sk-proj-...",
    "model": "gpt-4o-mini",
    "apiBase": "https://api.openai.com/v1"
  }
}
```

**响应**: `201 Created`

### 更新数据源

```http
PUT /api/datasource/{id}
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "name": "OpenAI Updated",
  "config": {
    "model": "gpt-4o"
  }
}
```

**响应**: `200 OK`

### 删除数据源

```http
DELETE /api/datasource/{id}
Authorization: Bearer {token}
```

**响应**: `204 No Content`

### 验证API密钥

```http
POST /api/datasource/{id}/validate
Authorization: Bearer {token}
Content-Type: application/json
```

**请求体**:

```json
{
  "apiKey": "sk-proj-..."
}
```

**响应**: `200 OK`

```json
{
  "valid": true,
  "message": "API密钥有效"
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

## 更新日志

### v1.0.0 (2025-02-10)

- ✨ 用户认证系统
- ✨ 题库管理
- ✨ 题目管理(扁平化路由)
- ✨ AI题目生成
- ✨ 答题记录
- ✨ 数据源管理

## 反馈和支持

- 📖 [完整文档](../README.md)
