# 安装部署指南

本文档详细说明AI Question Bank的安装部署步骤。

## 目录

- [环境要求](#环境要求)
- [快速部署](#快速部署)
- [详细配置](#详细配置)
- [生产环境部署](#生产环境部署)
- [数据备份与恢复](#数据备份与恢复)
- [常见问题](#常见问题)

## 环境要求

### 硬件要求

- **CPU**: 2核心或更高
- **内存**: 4GB RAM或更高(推荐8GB)
- **磁盘**: 20GB可用空间(用于数据库和日志)

### 软件要求

- **Docker**: 20.10或更高版本
- **Docker Compose**: 2.0或更高版本
- **操作系统**:
  - Linux (推荐Ubuntu 20.04+)
  - Windows 10/11 (WSL2)
  - macOS 11+

### 检查环境

```bash
# 检查Docker版本
docker --version

# 检查Docker Compose版本
docker-compose --version

# 检查Docker服务状态
docker ps
```

## 快速部署

### 步骤1: 获取代码

```bash
# 克隆仓库
git clone https://github.com/your-username/ai-questionbank.git
cd ai-questionbank

# 或下载发布版本
wget https://github.com/your-username/ai-questionbank/releases/download/v0.1.0-alpha/ai-questionbank-v0.1.0-alpha.tar.gz
tar -xzf ai-questionbank-v0.1.0-alpha.tar.gz
cd ai-questionbank
```

### 步骤2: 配置环境变量

```bash
# 复制环境变量模板
cp .env.example .env

# 编辑.env文件(至少修改以下关键配置)
# - JWT_SECRET: JWT密钥(至少32字符)
# - ENCRYPTION_KEY: API密钥加密密钥(至少32字符)
# - POSTGRES_PASSWORD: 数据库密码
# - ADMIN_PASSWORD: 管理员密码(如果关闭注册)

# 使用vim/nano编辑
vim .env
```

**最小配置示例**:

```bash
# 必须修改的安全配置
JWT_SECRET=change-this-to-a-random-32-character-string
ENCRYPTION_KEY=change-this-to-another-random-32-character-string
POSTGRES_PASSWORD=your-secure-database-password
ADMIN_PASSWORD=YourSecure@Password123

# AI配置(可选,也可以在Web界面中配置)
OPENAI_API_KEY=sk-your-openai-api-key
```

### 步骤3: 启动服务

```bash
# 后台启动所有服务
docker-compose up -d

# 查看启动日志
docker-compose logs -f

# 等待所有服务健康检查通过
docker-compose ps
```

预期输出:

```
NAME                          STATUS
ai-questionbank-db            up (healthy)
ai-questionbank-backend       up (healthy)
ai-questionbank-frontend      up (healthy)
```

### 步骤4: 访问应用

```bash
# 前端界面
open http://localhost:3000

# 后端Swagger API文档
open http://localhost:5000/swagger
```

**首次使用**:

1. 如果 `ENABLE_REGISTRATION=true`,访问注册页面创建账户
2. 如果 `ENABLE_REGISTRATION=false`,使用管理员账户登录
   - 邮箱: `.env` 中的 `ADMIN_EMAIL`
   - 密码: `.env` 中的 `ADMIN_PASSWORD`
3. 登录后前往 **设置 → AI配置** 添加AI API密钥

## 详细配置

### 数据库选择

#### 开发环境 - SQLite

```bash
# 修改.env文件
DB_TYPE=Sqlite
ConnectionStrings__DefaultConnection=Data Source=app.db

# 启动时不需要数据库服务
docker-compose up -d backend frontend
```

#### 生产环境 - PostgreSQL

```bash
# 使用默认配置即可
DB_TYPE=PostgreSQL
ConnectionStrings__DefaultConnection=Host=db;Database=questionbank;Username=postgres;Password=your_password

# 启动所有服务
docker-compose up -d
```

### AI Provider配置

#### OpenAI

```bash
# .env配置
DEFAULT_AI_PROVIDER=openai
OPENAI_API_KEY=sk-your-key-here
OPENAI_MODEL=gpt-4o-mini
OPENAI_API_BASE=https://api.openai.com/v1

# 或在Web界面: 设置 → AI配置 → 添加OpenAI配置
```

#### 通义千问

```bash
# .env配置
DEFAULT_AI_PROVIDER=qwen
QWEN_API_KEY=sk-your-qwen-key-here
QWEN_MODEL=qwen-turbo
QWEN_API_BASE=https://dashscope.aliyuncs.com/compatible-mode/v1

# 或在Web界面: 设置 → AI配置 → 添加通义千问配置
```

### 单用户模式

如果仅供个人使用,可以关闭注册功能:

```bash
# .env配置
ENABLE_REGISTRATION=false
ADMIN_EMAIL=your-email@example.com
ADMIN_PASSWORD=YourSecure@Password123
```

### 端口配置

修改默认端口:

```bash
# .env配置
BACKEND_PORT=5000      # 后端API端口
FRONTEND_PORT=3000     # 前端Web端口
POSTGRES_PORT=5432     # 数据库端口(仅开发需要暴露)
```

## 生产环境部署

### 域名配置

```bash
# .env配置
FRONTEND_URL=https://your-domain.com
VITE_API_BASE_URL=https://api.your-domain.com
```

### HTTPS配置

#### 方案1: 使用Nginx反向代理(推荐)

```bash
# 安装certbot获取SSL证书
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d your-domain.com

# 配置Nginx
sudo vim /etc/nginx/sites-available/ai-questionbank
```

Nginx配置示例:

```nginx
# HTTP重定向到HTTPS
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

# HTTPS配置
server {
    listen 443 ssl http2;
    server_name your-domain.com;

    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # 前端代理
    location / {
        proxy_pass http://localhost:3000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 后端API代理
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # WebSocket支持
    location /ws/ {
        proxy_pass http://localhost:5000/ws/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

```bash
# 启用配置并重启Nginx
sudo ln -s /etc/nginx/sites-available/ai-questionbank /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

#### 方案2: 内置HTTPS(不推荐生产使用)

```bash
# .env配置
HTTPS_ENABLED=true
SSL_CERT_PATH=/https/cert.pfx
SSL_CERT_PASSWORD=your-pfx-password

# 生成自签名证书(仅测试)
openssl pkcs12 -export -out cert.pfx -inkey key.pem -in cert.pem

# 或使用Let's Encrypt证书转换
openssl pkcs12 -export -out cert.pfx -inkey /etc/letsencrypt/live/your-domain.com/privkey.pem -in /etc/letsencrypt/live/your-domain.com/cert.pem -certfile /etc/letsencrypt/live/your-domain.com/chain.pem
```

### 性能优化

```bash
# .env配置
# 数据库连接池
DB_CONNECTION_POOL_SIZE=100

# 速率限制
RATE_LIMIT_PER_MINUTE=120
AI_GENERATION_LIMIT_PER_HOUR=20

# 日志级别(生产环境使用Warning)
LOG_LEVEL=Warning

# 关闭Swagger
ENABLE_SWAGGER=false
DETAILED_ERRORS=false
```

### 安全加固

```bash
# 修改所有默认密码
JWT_SECRET=$(openssl rand -base64 32)
ENCRYPTION_KEY=$(openssl rand -base64 32)
POSTGRES_PASSWORD=$(openssl rand -base64 16)

# 关闭注册(如需要)
ENABLE_REGISTRATION=false

# 启用HTTPS
HTTPS_ENABLED=true

# 限制CORS
CORS_ALLOW_ALL_ORIGINS=false
```

### 防火墙配置

```bash
# UFW防火墙
sudo ufw allow 80/tcp    # HTTP
sudo ufw allow 443/tcp   # HTTPS
sudo ufw enable

# 仅允许本地访问数据库
sudo ufw deny 5432/tcp
```

## 数据备份与恢复

### 备份数据

```bash
# 备份数据库
docker-compose exec db pg_dump -U postgres questionbank > backup_$(date +%Y%m%d_%H%M%S).sql

# 备份完整数据卷
docker run --rm -v ai-questionbank-postgres-data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_backup_$(date +%Y%m%d).tar.gz -C /data .

# 备份环境配置
cp .env .env.backup
```

### 恢复数据

```bash
# 恢复数据库
docker-compose exec -T db psql -U postgres questionbank < backup_20240101_120000.sql

# 恢复完整数据卷
docker run --rm -v ai-questionbank-postgres-data:/data -v $(pwd):/backup alpine tar xzf /backup/postgres_backup_20240101.tar.gz -C /data

# 重启服务
docker-compose restart db
```

### 自动备份脚本

```bash
#!/bin/bash
# backup.sh - 每日自动备份脚本

BACKUP_DIR="/path/to/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# 创建备份目录
mkdir -p $BACKUP_DIR

# 备份数据库
docker-compose exec -T db pg_dump -U postgres questionbank > $BACKUP_DIR/db_$DATE.sql

# 备份环境配置
cp .env $BACKUP_DIR/.env_$DATE

# 保留最近7天的备份
find $BACKUP_DIR -name "db_*.sql" -mtime +7 -delete
find $BACKUP_DIR -name ".env_*" -mtime +7 -delete

echo "Backup completed: $DATE"
```

添加到crontab:

```bash
# 每天凌晨2点备份
0 2 * * * /path/to/backup.sh >> /var/log/ai-questionbank-backup.log 2>&1
```

## 升级

```bash
# 停止服务
docker-compose down

# 备份数据(重要!)
./backup.sh

# 拉取最新代码
git pull origin main

# 拉取最新镜像
docker-compose pull

# 重新构建并启动
docker-compose up -d --build

# 查看升级日志
docker-compose logs -f
```

## 卸载

```bash
# 停止并删除容器
docker-compose down

# 删除数据卷(危险!会删除所有数据!)
docker-compose down -v

# 删除镜像
docker rmi ai-questionbank-backend ai-questionbank-frontend

# 删除项目目录
cd ..
rm -rf ai-questionbank
```

## 常见问题

### 端口冲突

如果端口已被占用:

```bash
# 检查端口占用
sudo lsof -i :3000
sudo lsof -i :5000

# 修改.env中的端口配置
FRONTEND_PORT=3001
BACKEND_PORT=5001
```

### 数据库连接失败

```bash
# 检查数据库健康状态
docker-compose logs db
docker-compose ps

# 等待数据库启动完成
docker-compose up -d db
docker-compose logs -f db

# 确认数据库可连接
docker-compose exec db psql -U postgres -d questionbank -c "SELECT 1"
```

### AI生成失败

```bash
# 检查API密钥配置
docker-compose logs backend | grep "AI"

# 验证API密钥
curl https://api.openai.com/v1/models \
  -H "Authorization: Bearer YOUR_API_KEY"

# 增加超时时间
AI_GENERATION_TIMEOUT=600
```

### 容器无法启动

```bash
# 查看详细日志
docker-compose logs backend
docker-compose logs frontend

# 重新构建镜像
docker-compose build --no-cache backend frontend

# 清理并重启
docker-compose down -v
docker-compose up -d
```

更多问题请参考 [FAQ文档](faq.md)。
