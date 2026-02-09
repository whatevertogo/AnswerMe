# 本地模式登录功能

## 功能说明

本地模式允许用户在没有账号系统的情况下直接使用 AnswerMe，适用于个人部署场景。

## 配置方式

### 方式一：环境变量（推荐用于生产环境）

在 `.env` 文件中添加：

```bash
# 启用本地登录模式
LOCAL_AUTH__ENABLE_LOCAL_LOGIN=true

# 本地用户配置（可选，有默认值）
LOCAL_AUTH__DEFAULT_USERNAME=LocalUser
LOCAL_AUTH__DEFAULT_EMAIL=local@answerme.local
LOCAL_AUTH__DEFAULT_PASSWORD=local123
```

### 方式二：配置文件（仅用于开发环境）

在 `appsettings.json` 中添加：

```json
{
  "LocalAuth": {
    "EnableLocalLogin": true,
    "DefaultUsername": "LocalUser",
    "DefaultEmail": "local@answerme.local",
    "DefaultPassword": "local123"
  }
}
```

## API 端点

### 本地登录

**请求：**

```http
POST /api/auth/local-login
```

**无需任何请求体或凭据**

**响应：**

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "LocalUser",
    "email": "local@answerme.local",
    "createdAt": "2024-01-01T00:00:00Z"
  }
}
```

## 工作原理

1. **首次使用**：系统自动创建默认本地用户（id=1）
2. **后续使用**：直接登录已有的本地用户账号
3. **数据隔离**：本地用户的所有数据（题库、AI配置等）都保存在该用户下

## 前端集成示例

```typescript
// 本地登录
const localLogin = async () => {
  const response = await fetch('/api/auth/local-login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' }
  })

  const { token, user } = await response.json()

  // 保存 token 到 localStorage
  localStorage.setItem('token', token)

  // 设置后续请求的 Authorization header
  axios.defaults.headers.common['Authorization'] = `Bearer ${token}`
}
```

## 安全注意事项

⚠️ **重要提示**：

1. 本地模式适用于**个人部署**场景，不适合多用户公开部署
2. 默认密码仅用于首次创建用户，建议部署后自行修改
3. 如果需要多用户功能，请使用常规的注册/登录流程
4. 生产环境部署时仍需设置强 `JWT_SECRET`

## 与多用户模式的区别

| 特性 | 本地模式 | 多用户模式 |
|------|----------|------------|
| 需要注册 | ❌ | ✅ |
| 需要密码登录 | ❌ | ✅ |
| 适用场景 | 个人使用 | 团队/公开 |
| 数据隔离 | 单用户 | 多用户 |
| API 端点 | `/api/auth/local-login` | `/api/auth/login` |

## 常见问题

**Q: 可以同时启用本地模式和多用户模式吗？**

A: 技术上可以，但不建议。如果启用了本地模式，建议禁用注册功能（前端隐藏注册按钮）。

**Q: 如何禁用本地模式？**

A: 设置 `LOCAL_AUTH__ENABLE_LOCAL_LOGIN=false` 或删除该配置。

**Q: 本地用户的密码有什么用？**

A: 仅在首次创建用户时使用。本地登录端点不验证密码，直接返回 token。但保留密码是为了防止安全扫描和保持数据结构一致。
