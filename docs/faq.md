# 常见问题 (FAQ)

本文档收集了AI Question Bank的常见问题和解决方案。

## 目录

- [安装问题](#安装问题)
- [配置问题](#配置问题)
- [数据库问题](#数据库问题)
- [AI生成问题](#ai生成问题)
- [性能问题](#性能问题)
- [安全问题](#安全问题)
- [备份与恢复](#备份与恢复)

## 安装问题

### Q1: Docker版本过低怎么办?

**错误信息**:
```
ERROR: Docker version 20.10+ is required
```

**解决方案**:

```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# CentOS/RHEL
sudo yum install -y docker-ce docker-ce-cli containerd.io

# 验证版本
docker --version
```

### Q2: 端口被占用怎么办?

**错误信息**:
```
ERROR: for frontend  Bind for 0.0.0.0:3000 failed: port is already allocated
```

**解决方案**:

```bash
# 方法1: 停止占用端口的服务
sudo lsof -i :3000
sudo kill -9 <PID>

# 方法2: 修改配置端口
vim .env
# 添加:
FRONTEND_PORT=3001
BACKEND_PORT=5001

# 重启服务
docker-compose up -d
```

### Q3: 权限不足怎么办?

**错误信息**:
```
ERROR: permission denied while trying to connect to the Docker daemon
```

**解决方案**:

```bash
# 将用户添加到docker组
sudo usermod -aG docker $USER

# 重新登录或执行
newgrp docker

# 验证
docker ps
```

### Q4: 容器启动失败怎么办?

**诊断步骤**:

```bash
# 1. 查看容器状态
docker-compose ps

# 2. 查看详细日志
docker-compose logs backend
docker-compose logs frontend
docker-compose logs db

# 3. 重新构建镜像
docker-compose build --no-cache

# 4. 清理并重启
docker-compose down -v
docker-compose up -d
```

## 配置问题

### Q5: 环境变量不生效怎么办?

**原因**:
- .env文件格式错误
- 环境变量未重新加载
- Docker缓存

**解决方案**:

```bash
# 1. 检查.env文件格式
cat .env
# 确保没有引号,没有空格在等号周围

# 错误示例:
# JWT_SECRET = "value"
# OPENAI_API_KEY="sk-xxx"

# 正确示例:
# JWT_SECRET=value
# OPENAI_API_KEY=sk-xxx

# 2. 验证配置
docker-compose config

# 3. 完全重启
docker-compose down
docker-compose up -d
```

### Q6: CORS错误怎么办?

**错误信息**:
```
Access to XMLHttpRequest at 'http://localhost:5000/api/...' from origin 'http://localhost:3000' has been blocked by CORS policy
```

**解决方案**:

```bash
# 开发环境: 允许所有来源
vim .env
# 添加:
CORS_ALLOW_ALL_ORIGINS=true

# 生产环境: 配置正确的前端URL
vim .env
# 修改:
FRONTEND_URL=https://your-domain.com
CORS_ALLOW_ALL_ORIGINS=false

# 重启服务
docker-compose restart backend
```

### Q7: JWT认证失败怎么办?

**错误信息**:
```
401 Unauthorized
Invalid token
```

**解决方案**:

```bash
# 1. 检查JWT_SECRET配置
docker-compose exec backend env | grep JWT_SECRET

# 2. 重新生成JWT_SECRET
JWT_SECRET=$(openssl rand -base64 32)
echo "JWT_SECRET=$JWT_SECRET" >> .env

# 3. 清除浏览器缓存并重新登录
# - 打开浏览器开发者工具(F12)
# - Application → Storage → Clear site data
# - 重新登录
```

## 数据库问题

### Q8: 数据库连接失败怎么办?

**错误信息**:
```
Npgsql.PostgresException: could not connect to server
```

**诊断步骤**:

```bash
# 1. 检查数据库容器状态
docker-compose ps db

# 2. 检查数据库日志
docker-compose logs db

# 3. 等待数据库启动完成
docker-compose up -d db
docker-compose logs -f db
# 看到 "database system is ready to accept connections" 表示启动成功

# 4. 测试数据库连接
docker-compose exec db psql -U postgres -d questionbank -c "SELECT 1"
```

**解决方案**:

```bash
# 检查连接字符串
vim .env
# 确认:
# ConnectionStrings__DefaultConnection=Host=db;Database=questionbank;Username=postgres;Password=your_password

# 检查数据库密码
docker-compose exec db psql -U postgres -d questionbank
# 如果提示密码错误,修改.env中的POSTGRES_PASSWORD
```

### Q9: 数据库迁移失败怎么办?

**错误信息**:
```
Applying migration '20240101000000_Init' failed
```

**解决方案**:

```bash
# 1. 查看迁移失败日志
docker-compose logs backend | grep -i migration

# 2. 手动执行迁移
docker-compose exec backend dotnet ef database update

# 3. 如果迁移冲突,重置数据库(警告:会删除数据!)
docker-compose exec backend dotnet ef database drop --force
docker-compose exec backend dotnet ef database update
```

### Q10: 如何从SQLite迁移到PostgreSQL?

**步骤**:

```bash
# 1. 备份SQLite数据
docker-compose exec backend cp /app/app.db /app/app.db.backup

# 2. 导出SQLite数据为SQL
docker-compose exec backend sqlite3 app.db .dump > export.sql

# 3. 修改.env配置
vim .env
# 修改:
DB_TYPE=PostgreSQL
ConnectionStrings__DefaultConnection=Host=db;Database=questionbank;Username=postgres;Password=password

# 4. 重启服务
docker-compose down
docker-compose up -d

# 5. 导入数据(需要手动处理类型差异)
docker-compose exec -T db psql -U postgres questionbank < export.sql
```

**注意**: SQLite和PostgreSQL有类型差异,建议使用提供的迁移脚本:

```bash
./scripts/migrate-sqlite-to-postgres.sh
```

## AI生成问题

### Q11: AI生成失败怎么办?

**错误信息**:
```
AI generation failed: API key invalid
```

**诊断步骤**:

```bash
# 1. 检查API密钥配置
docker-compose logs backend | grep "AI"

# 2. 验证API密钥(OpenAI)
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer $OPENAI_API_KEY"

# 3. 验证API密钥(通义千问)
curl https://dashscope.aliyuncs.com/api/v1/services/aigc/text-generation/generation \
  -H "Authorization: Bearer $QWEN_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{"model":"qwen-turbo","input":{"prompt":"test"}}'
```

**解决方案**:

```bash
# 1. 在Web界面重新配置API密钥
# 登录 → 设置 → AI配置 → 编辑配置

# 2. 或在.env中修改
vim .env
# 修改:
OPENAI_API_KEY=sk-your-correct-key

# 3. 重启后端服务
docker-compose restart backend
```

### Q12: AI生成超时怎么办?

**错误信息**:
```
AI generation timeout after 300 seconds
```

**解决方案**:

```bash
# 1. 增加超时时间
vim .env
# 修改:
AI_GENERATION_TIMEOUT=600

# 2. 减少生成题目数量
# 生成10题而不是100题

# 3. 检查网络连接
docker-compose exec backend ping api.openai.com

# 4. 重启服务
docker-compose restart backend
```

### Q13: 如何选择合适的AI模型?

**推荐配置**:

**OpenAI**:
- `gpt-4o-mini`: 推荐用于大多数场景,速度快,价格便宜
- `gpt-4o`: 复杂题目,需要更高准确率
- `gpt-3.5-turbo`: 预算有限时的选择

**通义千问**:
- `qwen-turbo`: 速度快,适合简单题目
- `qwen-plus`: 准确率更高
- `qwen-max`: 最复杂场景

```bash
# 修改模型
vim .env
OPENAI_MODEL=gpt-4o-mini
QWEN_MODEL=qwen-turbo
```

## 性能问题

### Q14: 应用响应慢怎么办?

**诊断步骤**:

```bash
# 1. 检查资源使用
docker stats

# 2. 检查数据库性能
docker-compose exec db psql -U postgres -d questionbank -c "SELECT * FROM pg_stat_activity"

# 3. 检查慢查询
docker-compose logs backend | grep "Duration"
```

**解决方案**:

```bash
# 1. 增加数据库连接池
vim .env
DB_CONNECTION_POOL_SIZE=200

# 2. 启用查询缓存
# (需要应用层面配置)

# 3. 增加容器资源限制
vim docker-compose.yml
# 添加:
services:
  backend:
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G

# 4. 使用更快的模型
OPENAI_MODEL=gpt-4o-mini  # 而不是gpt-4o
```

### Q15: 内存占用过高怎么办?

**诊断步骤**:

```bash
# 查看内存使用
docker stats --no-stream

# 查看容器内存限制
docker inspect ai-questionbank-backend | grep -i memory
```

**解决方案**:

```bash
# 1. 限制容器内存
vim docker-compose.yml
services:
  backend:
    mem_limit: 1g
    memswap_limit: 1g

# 2. 清理旧日志
docker-compose exec backend rm /app/logs/log-*.txt

# 3. 调整数据库缓存
vim .env
# 添加:
POSTGRES_SHARED_BUFFERS=256MB
POSTGRES_EFFECTIVE_CACHE_SIZE=1GB
```

## 安全问题

### Q16: 如何加固生产环境安全?

**安全检查清单**:

```bash
# 1. 修改所有默认密钥
JWT_SECRET=$(openssl rand -base64 32)
ENCRYPTION_KEY=$(openssl rand -base64 32)
POSTGRES_PASSWORD=$(openssl rand -base64 16)
ADMIN_PASSWORD=$(openssl rand -base64 16)

# 2. 启用HTTPS
vim .env
HTTPS_ENABLED=true

# 3. 关闭注册(如需要)
ENABLE_REGISTRATION=false

# 4. 限制CORS
CORS_ALLOW_ALL_ORIGINS=false
FRONTEND_URL=https://your-domain.com

# 5. 关闭Swagger
ENABLE_SWAGGER=false

# 6. 调整日志级别
LOG_LEVEL=Warning

# 7. 关闭详细错误
DETAILED_ERRORS=false
```

### Q17: API密钥存储安全吗?

**安全措施**:

1. **加密存储**: API密钥使用AES-256加密存储
2. **密钥隔离**: 加密密钥从环境变量读取,不在代码中
3. **前端隔离**: API密钥永远不会返回给前端
4. **日志脱敏**: 日志中不记录API密钥

**验证**:

```bash
# 1. 检查数据库中是否为加密存储
docker-compose exec db psql -U postgres -d questionbank -c "SELECT id, provider, api_key_encrypted FROM user_ai_configs"

# 2. 确认前端无法获取密钥
# 打开浏览器开发者工具 → Network
# 查看API响应,确认不包含api_key字段
```

### Q18: 如何设置防火墙?

**UFW (Ubuntu)**:

```bash
# 启用防火墙
sudo ufw enable

# 允许HTTP/HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# 拒绝数据库外部访问
sudo ufw deny 5432/tcp

# 查看状态
sudo ufw status
```

**firewalld (CentOS)**:

```bash
# 启用防火墙
sudo systemctl start firewalld
sudo systemctl enable firewalld

# 允许HTTP/HTTPS
sudo firewall-cmd --permanent --add-service=http
sudo firewall-cmd --permanent --add-service=https

# 拒绝数据库外部访问
sudo firewall-cmd --permanent --remove-port=5432/tcp

# 重载配置
sudo firewall-cmd --reload
```

## 备份与恢复

### Q19: 如何备份数据?

**完整备份脚本**:

```bash
#!/bin/bash
# backup.sh

BACKUP_DIR="./backups"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p $BACKUP_DIR

# 备份数据库
docker-compose exec -T db pg_dump -U postgres questionbank > $BACKUP_DIR/db_$DATE.sql

# 备份环境配置
cp .env $BACKUP_DIR/.env_$DATE

# 压缩备份
tar czf $BACKUP_DIR/backup_$DATE.tar.gz $BACKUP_DIR/db_$DATE.sql $BACKUP_DIR/.env_$DATE

# 清理临时文件
rm $BACKUP_DIR/db_$DATE.sql $BACKUP_DIR/.env_$DATE

# 保留最近7天
find $BACKUP_DIR -name "backup_*.tar.gz" -mtime +7 -delete

echo "Backup completed: backup_$DATE.tar.gz"
```

**使用**:

```bash
chmod +x backup.sh
./backup.sh
```

### Q20: 如何恢复数据?

**恢复步骤**:

```bash
# 1. 停止服务
docker-compose down

# 2. 解压备份
tar xzf backups/backup_20240101_120000.tar.gz -C backups/

# 3. 启动数据库
docker-compose up -d db

# 4. 等待数据库就绪
docker-compose logs -f db
# 等待看到 "database system is ready to accept connections"

# 5. 恢复数据库
docker-compose exec -T db psql -U postgres questionbank < backups/db_20240101_120000.sql

# 6. 恢复环境配置
cp backups/.env_20240101_120000 .env

# 7. 启动所有服务
docker-compose up -d

# 8. 验证
docker-compose ps
```

### Q21: 忘记管理员密码怎么办?

**解决方案**:

```bash
# 1. 连接数据库
docker-compose exec db psql -U postgres -d questionbank

# 2. 重置密码为 Admin@123456
UPDATE users SET password_hash='$2a$10$YourNewHashedPassword' WHERE email = 'admin@example.com';

# 或在.env中设置新密码后重新创建管理员
vim .env
# 修改:
ADMIN_PASSWORD=NewSecure@Password123

# 重启服务
docker-compose restart backend
```

## 其他问题

### Q22: 如何查看实时日志?

```bash
# 查看所有服务日志
docker-compose logs -f

# 查看特定服务日志
docker-compose logs -f backend
docker-compose logs -f frontend
docker-compose logs -f db

# 查看最近100行
docker-compose logs --tail=100 backend

# 查看特定时间范围
docker-compose logs --since="2024-01-01T00:00:00" --until="2024-01-01T23:59:59" backend
```

### Q23: 如何升级到最新版本?

**步骤**:

```bash
# 1. 备份数据(重要!)
./backup.sh

# 2. 停止服务
docker-compose down

# 3. 拉取最新代码
git pull origin main

# 4. 拉取最新镜像
docker-compose pull

# 5. 重新构建并启动
docker-compose up -d --build

# 6. 执行数据库迁移
docker-compose exec backend dotnet ef database update

# 7. 验证
docker-compose ps
docker-compose logs -f
```

### Q24: 如何完全卸载?

**步骤**:

```bash
# 1. 停止并删除容器
docker-compose down

# 2. 删除数据卷(危险!会删除所有数据)
docker-compose down -v

# 3. 删除镜像
docker rmi ai-questionbank-backend ai-questionbank-frontend postgres:16-alpine

# 4. 删除项目目录
cd ..
rm -rf ai-questionbank
```

### Q25: 如何获取帮助?

**资源**:

- 📖 [文档](docs/installation.md)
- 💬 [GitHub Discussions](https://github.com/your-username/ai-questionbank/discussions)
- 🐛 [Bug报告](https://github.com/your-username/ai-questionbank/issues)
- 📧 Email: support@example.com

**报告问题时请提供**:

- 版本号: `docker-compose exec backend dotnet --version`
- 错误日志: `docker-compose logs backend`
- 配置信息(删除敏感信息)
- 复现步骤
- 系统环境: OS, Docker版本
