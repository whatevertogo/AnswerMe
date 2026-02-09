# Docker 安全配置指南

## 环境变量安全

### 当前实现
AnswerMe 使用 `.env` 文件管理敏感环境变量。`.env` 文件已被加入 `.gitignore`，不会被提交到版本控制。

### 安全最佳实践

#### 1. 生产环境部署

**选项 A: 使用 Docker Secrets (推荐用于 Swarm 模式)**

```bash
# 生成随机密钥
echo "your-random-jwt-secret-here" | docker secret create jwt_secret -

# 在 docker-compose.yml 中使用
version: '3.8'
services:
  backend:
    secrets:
      - jwt_secret
    environment:
      - JWT_SECRET_FILE=/run/secrets/jwt_secret

secrets:
  jwt_secret:
    external: true
```

**选项 B: 使用环境变量文件（当前方案）**

```bash
# 复制示例配置
cp .env.example .env

# 编辑 .env 文件，生成强密码
# JWT密钥: openssl rand -base64 64
# 数据库密码: openssl rand -base64 32
```

#### 2. 防止敏感信息泄露

✅ **已实施的安全措施**：
- `.env` 文件已加入 `.gitignore`
- `.env.example` 提供安全的模板
- 日志自动过滤敏感信息（见 P0-3 修复）
- DEBUG 模式默认禁用敏感数据日志

⚠️ **使用注意事项**：
1. 确保 `.env` 文件权限正确：`chmod 600 .env`
2. 定期轮换密钥（建议每 90 天）
3. 不要在生产环境使用示例密码
4. 审计日志文件，确保无敏感信息泄露

#### 3. 验证安全配置

```bash
# 检查 .env 文件权限
ls -la .env

# 确认日志中无敏感信息
grep -i "password\|secret\|apikey" logs/answerme-*.log

# 验证容器环境变量（应只看到变量名，不是值）
docker inspect answerme-backend | jq '.[0].Config.Env'
```

## 容器安全加固

### 已实施措施
- ✅ 非root用户运行（Dockerfile 配置）
- ✅ 只读根文件系统（可选）
- ✅ 资源限制（CPU、内存）
- ✅ 健康检查
- ✅ 日志大小限制

### 运行时安全

```bash
# 扫描镜像漏洞
docker scan answerme-backend:latest

# 使用安全的基础镜像
# 当前使用: mcr.microsoft.com/dotnet/aspnet:10.0
# 定期更新基础镜像版本

# 限制容器能力
docker run --cap-drop=ALL --cap-add=NET_BIND_SERVICE ...
```

## 网络安全

- ✅ 使用隔离的 Docker 网络（answerme-network）
- ✅ 只暴露必要的端口
- ✅ CORS 限制允许的来源

## 密钥管理

### Data Protection API 密钥

✅ **已优化**：
- 密钥持久化到 `backend-keys` Docker 卷
- 应用重启后仍可解密数据
- 密钥生命周期：90天
- 自动密钥轮换

位置：
- Docker: `backend-keys` 卷
- 本地: `backend/AnswerMe.API/keys/` 目录

## 参考

- [Docker Secrets 文档](https://docs.docker.com/engine/swarm/secrets/)
- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [OWASP Docker Security Checklist](https://cheatsheetseries.owasp.org/cheatsheets/Docker_Security_Cheat_Sheet.html)
