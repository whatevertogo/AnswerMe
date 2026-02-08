# 配置参考

完整的环境变量配置说明。

## 目录

- [快速配置](#快速配置)
- [应用配置](#应用配置)
- [数据库配置](#数据库配置)
- [安全配置](#安全配置)
- [AI配置](#ai配置)
- [性能配置](#性能配置)
- [日志配置](#日志配置)
- [邮件配置](#邮件配置)

## 快速配置

### 最小配置

只需修改以下5个配置即可启动:

```bash
# .env文件
JWT_SECRET=your-super-secret-jwt-key-at-least-32-characters-long
ENCRYPTION_KEY=your-encryption-key-32-chars-minimum
POSTGRES_PASSWORD=your_secure_database_password
ADMIN_PASSWORD=YourSecure@Password123

# AI配置(可选,也可在Web界面配置)
OPENAI_API_KEY=sk-your-openai-api-key
```

### 配置优先级

1. 环境变量(.env文件)
2. appsettings.Production.json
3. appsettings.json
4. 代码默认值

## 应用配置

### ASPNETCORE_ENVIRONMENT

应用运行环境。

- **类型**: String
- **可选值**: `Development`, `Production`, `Staging`
- **默认值**: `Production`
- **说明**:
  - `Development`: 开发模式,显示详细错误,启用Swagger
  - `Production`: 生产模式,优化性能,隐藏敏感信息
  - `Staging`: 预发布环境

```bash
ASPNETCORE_ENVIRONMENT=Production
```

### ASPNETCORE_URLS

应用监听的URL。

- **类型**: String
- **默认值**: `http://+:5000`
- **说明**: 通常不需要修改

```bash
ASPNETCORE_URLS=http://+:5000
```

### FRONTEND_URL

前端访问地址(用于CORS配置)。

- **类型**: String (URL)
- **默认值**: `http://localhost:3000`
- **说明**: 生产环境需设置为实际域名

```bash
# 开发环境
FRONTEND_URL=http://localhost:3000

# 生产环境
FRONTEND_URL=https://your-domain.com
```

## 数据库配置

### DB_TYPE

数据库类型。

- **类型**: String
- **可选值**: `Sqlite`, `PostgreSQL`
- **默认值**: `PostgreSQL`
- **说明**: SQLite适合开发,PostgreSQL适合生产

```bash
# 开发环境
DB_TYPE=Sqlite

# 生产环境
DB_TYPE=PostgreSQL
```

### ConnectionStrings__DefaultConnection

数据库连接字符串。

- **类型**: String (Connection String)
- **必需**: 是

**SQLite**:

```bash
ConnectionStrings__DefaultConnection=Data Source=app.db
```

**PostgreSQL**:

```bash
# 基本格式
ConnectionStrings__DefaultConnection=Host=db;Database=questionbank;Username=postgres;Password=password

# 完整格式
ConnectionStrings__DefaultConnection=Host=db;Port=5432;Database=questionbank;Username=postgres;Password=password;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100

# 参数说明:
# - Host: 数据库主机(Docker服务名)
# - Port: 端口(默认5432)
# - Database: 数据库名
# - Username: 用户名
# - Password: 密码
# - Pooling: 是否启用连接池(默认true)
# - Minimum Pool Size: 最小连接数(默认5)
# - Maximum Pool Size: 最大连接数(默认100)
```

## 安全配置

### JWT_SECRET

JWT Token签名密钥。

- **类型**: String
- **必需**: 是
- **最小长度**: 32字符
- **警告**: 生产环境必须修改为随机字符串

```bash
# 生成随机密钥
JWT_SECRET=$(openssl rand -base64 32)

# 或使用在线工具生成
# https://generate-random.org/encryption-key-generator
```

### JWT_EXPIRATION_HOURS

JWT Token过期时间。

- **类型**: Integer (小时)
- **默认值**: `168` (7天)
- **范围**: 1-8760

```bash
# 短期Token(1天)
JWT_EXPIRATION_HOURS=24

# 长期Token(30天)
JWT_EXPIRATION_HOURS=720
```

### ENCRYPTION_KEY

API密钥加密密钥(AES-256)。

- **类型**: String
- **必需**: 是
- **最小长度**: 32字符
- **警告**: 生产环境必须修改
- **说明**: 用于加密用户配置的AI API密钥

```bash
# 生成随机密钥
ENCRYPTION_KEY=$(openssl rand -base64 32)
```

### ENABLE_REGISTRATION

是否开启用户注册。

- **类型**: Boolean
- **默认值**: `true`
- **说明**: 关闭后进入单用户模式

```bash
# 多用户模式
ENABLE_REGISTRATION=true

# 单用户模式(个人使用)
ENABLE_REGISTRATION=false
ADMIN_EMAIL=admin@example.com
ADMIN_PASSWORD=Admin@123456
```

### HTTPS_ENABLED

是否强制HTTPS。

- **类型**: Boolean
- **默认值**: `false`
- **说明**: 生产环境建议启用

```bash
HTTPS_ENABLED=true
SSL_CERT_PATH=/https/cert.pfx
SSL_CERT_PASSWORD=your-pfx-password
```

## AI配置

### DEFAULT_AI_PROVIDER

默认AI Provider。

- **类型**: String
- **可选值**: `openai`, `qwen`
- **默认值**: `openai`

```bash
DEFAULT_AI_PROVIDER=openai
```

### OpenAI配置

```bash
# API密钥(在 https://platform.openai.com/api-keys 获取)
OPENAI_API_KEY=sk-proj-xxxxxxxxxxxxxxxx

# 使用的模型
# - gpt-4o-mini (推荐,性价比高)
# - gpt-4o (更强但贵)
# - gpt-3.5-turbo (便宜)
OPENAI_MODEL=gpt-4o-mini

# API基础地址(兼容OpenAI格式的服务)
OPENAI_API_BASE=https://api.openai.com/v1

# Azure OpenAI配置
OPENAI_API_BASE=https://your-resource.openai.azure.com/
OPENAI_API_VERSION=2023-05-15
```

### 通义千问配置

```bash
# API密钥(在 https://dashscope.aliyun.com 获取)
QWEN_API_KEY=sk-xxxxxxxxxxxxxxxx

# 使用的模型
# - qwen-turbo (推荐,速度快)
# - qwen-plus (更强)
# - qwen-max (最强)
QWEN_MODEL=qwen-turbo

# API基础地址
QWEN_API_BASE=https://dashscope.aliyuncs.com/compatible-mode/v1
```

### AI_GENERATION_TIMEOUT

AI生成超时时间。

- **类型**: Integer (秒)
- **默认值**: `300` (5分钟)
- **说明**: 生成大量题目时可能需要更长时间

```bash
# 快速生成
AI_GENERATION_TIMEOUT=120

# 大批量生成
AI_GENERATION_TIMEOUT=600
```

## 性能配置

### RATE_LIMIT_PER_MINUTE

API速率限制。

- **类型**: Integer (请求/分钟)
- **默认值**: `60`
- **说明**: 防止API滥用

```bash
# 宽松限制
RATE_LIMIT_PER_MINUTE=120

# 严格限制
RATE_LIMIT_PER_MINUTE=30
```

### AI_GENERATION_LIMIT_PER_HOUR

AI生成频率限制。

- **类型**: Integer (请求/小时)
- **默认值**: `10`
- **说明**: 保护AI API配额

```bash
# 允许更多生成
AI_GENERATION_LIMIT_PER_HOUR=20
```

### DB_CONNECTION_POOL_SIZE

数据库连接池大小。

- **类型**: Integer
- **默认值**: `100`
- **说明**: 根据并发用户调整

```bash
# 低并发
DB_CONNECTION_POOL_SIZE=50

# 高并发
DB_CONNECTION_POOL_SIZE=200
```

## 日志配置

### LOG_LEVEL

日志级别。

- **类型**: String
- **可选值**: `Debug`, `Information`, `Warning`, `Error`, `Fatal`
- **默认值**: `Information`

```bash
# 开发环境
LOG_LEVEL=Debug

# 生产环境
LOG_LEVEL=Warning
```

### SERILOG配置

```bash
# 控制台输出
SERILOG_WRITE_TO_CONSOLE=true

# 文件输出
SERILOG_WRITE_TO_FILE=true
SERILOG_LOG_FILE_PATH=/app/logs/log-.txt

# 日志轮转
SERILOG_RETAINED_FILE_COUNT_LIMIT=30
SERILOG_ROLLING_INTERVAL=Day
```

## 邮件配置

### SMTP配置

```bash
# SMTP服务器
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587

# SMTP认证
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your-app-password

# 发件人信息
SMTP_FROM_EMAIL=noreply@your-domain.com
SMTP_FROM_NAME=AI Question Bank

# 启用TLS
SMTP_ENABLE_SSL=true
SMTP_USE_STARTTLS=true
```

### EMAIL_VERIFICATION_ENABLED

是否启用邮箱验证。

- **类型**: Boolean
- **默认值**: `false`
- **说明**: 需要先配置SMTP

```bash
EMAIL_VERIFICATION_ENABLED=true
```

## 高级配置

### HTTP_REQUEST_TIMEOUT

HTTP请求超时。

- **类型**: Integer (秒)
- **默认值**: `30`

```bash
HTTP_REQUEST_TIMEOUT=60
```

### MAX_UPLOAD_SIZE_MB

文件上传大小限制。

- **类型**: Integer (MB)
- **默认值**: `10`

```bash
MAX_UPLOAD_SIZE_MB=50
```

### SESSION_TIMEOUT_MINUTES

会话超时时间。

- **类型**: Integer (分钟)
- **默认值**: `30`

```bash
SESSION_TIMEOUT_MINUTES=60
```

### CORS配置

```bash
# 开发环境: 允许所有来源
CORS_ALLOW_ALL_ORIGINS=true

# 生产环境: 仅允许指定域名
CORS_ALLOW_ALL_ORIGINS=false
FRONTEND_URL=https://your-domain.com
```

## 环境特定配置

### 开发环境(.env.development)

```bash
ASPNETCORE_ENVIRONMENT=Development
DB_TYPE=Sqlite
ConnectionStrings__DefaultConnection=Data Source=app.db
CORS_ALLOW_ALL_ORIGINS=true
ENABLE_SWAGGER=true
DETAILED_ERRORS=true
LOG_LEVEL=Debug
```

### 生产环境(.env.production)

```bash
ASPNETCORE_ENVIRONMENT=Production
DB_TYPE=PostgreSQL
ConnectionStrings__DefaultConnection=Host=db;Database=questionbank;Username=postgres;Password=${POSTGRES_PASSWORD}
CORS_ALLOW_ALL_ORIGINS=false
ENABLE_SWAGGER=false
DETAILED_ERRORS=false
LOG_LEVEL=Warning
HTTPS_ENABLED=true
```

## 配置验证

启动前验证配置:

```bash
# 检查环境变量
docker-compose config

# 验证数据库连接
docker-compose run --rm backend dotnet ef database update

# 测试AI配置
curl -X POST http://localhost:5000/api/ai/validate \
  -H "Content-Type: application/json" \
  -d '{"provider":"openai","apiKey":"sk-test"}'
```

## 安全检查清单

部署前确认:

- [ ] JWT_SECRET已修改为随机字符串
- [ ] ENCRYPTION_KEY已修改为随机字符串
- [ ] POSTGRES_PASSWORD已设置为强密码
- [ ] ADMIN_PASSWORD已设置为强密码
- [ ] HTTPS_ENABLED=true (生产环境)
- [ ] CORS_ALLOW_ALL_ORIGINS=false (生产环境)
- [ ] ENABLE_SWAGGER=false (生产环境)
- [ ] DETAILED_ERRORS=false (生产环境)
- [ ] LOG_LEVEL=Warning (生产环境)
- [ ] API密钥已配置并测试

## 故障排查

### 配置不生效

```bash
# 1. 确认.env文件在正确位置
ls -la .env

# 2. 重建容器
docker-compose down
docker-compose up -d

# 3. 检查容器内环境变量
docker-compose exec backend env | grep JWT_SECRET
```

### 数据库连接失败

```bash
# 检查连接字符串格式
docker-compose logs backend | grep "Connection"

# 测试数据库连接
docker-compose exec db psql -U postgres -d questionbank
```

### AI调用失败

```bash
# 检查AI配置
docker-compose logs backend | grep "AI"

# 验证API密钥
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $OPENAI_API_KEY"
```

更多配置问题请参考 [FAQ文档](faq.md)。
