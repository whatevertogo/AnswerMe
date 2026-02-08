# 部署检查清单

在将AI Question Bank部署到生产环境之前,请完成以下检查清单。

## 📋 部署前检查

### 1. 安全配置

- [ ] **修改默认密钥**
  - [ ] JWT_SECRET已修改为随机32字符字符串
  - [ ] ENCRYPTION_KEY已修改为随机32字符字符串
  - [ ] POSTGRES_PASSWORD已设置为强密码
  - [ ] ADMIN_PASSWORD已设置为强密码(如启用单用户模式)

- [ ] **配置HTTPS**
  - [ ] HTTPS_ENABLED=true
  - [ ] SSL证书已配置
  - [ ] SSL_CERT_PATH和SSL_CERT_PASSWORD已设置

- [ ] **CORS配置**
  - [ ] CORS_ALLOW_ALL_ORIGINS=false
  - [ ] FRONTEND_URL设置为实际域名

- [ ] **敏感信息保护**
  - [ ] .env文件不在代码仓库中
  - [ ] .env.example已更新(移除真实密钥)
  - [ ] 日志不记录敏感信息(API密钥、密码等)

### 2. 应用配置

- [ ] **环境设置**
  - [ ] ASPNETCORE_ENVIRONMENT=Production
  - [ ] DB_TYPE=PostgreSQL
  - [ ] 数据库连接字符串正确

- [ ] **功能配置**
  - [ ] ENABLE_REGISTRATION根据需求设置
  - [ ] ADMIN_EMAIL和ADMIN_PASSWORD已配置(如关闭注册)
  - [ ] AI Provider已配置并测试

- [ ] **性能优化**
  - [ ] DB_CONNECTION_POOL_SIZE根据并发调整
  - [ ] RATE_LIMIT_PER_MINUTE根据需求设置
  - [ ] LOG_LEVEL=Warning(生产环境)

### 3. 开发功能关闭

- [ ] **Swagger**
  - [ ] ENABLE_SWAGGER=false

- [ ] **详细错误**
  - [ ] DETAILED_ERRORS=false

- [ ] **CORS**
  - [ ] CORS_ALLOW_ALL_ORIGINS=false

### 4. AI配置测试

- [ ] **OpenAI配置**(如使用)
  - [ ] OPENAI_API_KEY有效
  - [ ] 账户有足够配额
  - [ ] API_BASE可访问

- [ ] **通义千问配置**(如使用)
  - [ ] QWEN_API_KEY有效
  - [ ] 账户有足够配额
  - [ ] API_BASE可访问

## 🔧 基础设施检查

### 1. 服务器要求

- [ ] **硬件**
  - [ ] CPU: 2核心或更高
  - [ ] 内存: 4GB RAM或更高
  - [ ] 磁盘: 20GB可用空间

- [ ] **软件**
  - [ ] Docker 20.10+已安装
  - [ ] Docker Compose 2.0+已安装
  - [ ] Git已安装

### 2. 网络配置

- [ ] **域名**
  - [ ] 域名已指向服务器IP
  - [ ] DNS解析已生效

- [ ] **防火墙**
  - [ ] 80端口(HTTP)已开放
  - [ ] 443端口(HTTPS)已开放
  - [ ] 5432端口(PostgreSQL)仅内部访问
  - [ ] 其他端口已关闭

- [ ] **反向代理**(如使用)
  - [ ] Nginx/Caddy已配置
  - [ ] SSL证书已配置
  - [ ] 代理规则正确

### 3. 数据持久化

- [ ] **数据卷**
  - [ ] PostgreSQL数据卷已配置
  - [ ] 后端日志卷已配置
  - [ ] 数据卷有足够空间

- [ ] **备份计划**
  - [ ] 自动备份脚本已配置
  - [ ] 备份存储路径已设置
  - [ ] 备份保留策略已定义
  - [ ] 恢复流程已测试

## 🧪 功能测试

### 1. 用户功能

- [ ] **注册/登录**
  - [ ] 用户注册成功
  - [ ] 用户登录成功
  - [ ] Token认证正常
  - [ ] 密码重置功能正常(如启用)

- [ ] **AI配置**
  - [ ] 添加AI配置成功
  - [ ] API密钥验证正常
  - [ ] 密钥加密存储
  - [ ] 密钥不返回前端

### 2. 题库功能

- [ ] **题库管理**
  - [ ] 创建题库成功
  - [ ] 编辑题库成功
  - [ ] 删除题库成功
  - [ ] 搜索题库正常

- [ ] **题目管理**
  - [ ] 手动添加题目成功
  - [ ] 编辑题目成功
  - [ ] 删除题目成功
  - [ ] 分页查询正常

### 3. AI生成

- [ ] **生成功能**
  - [ ] 小批量生成(≤20题)成功
  - [ ] 大批量生成(>20题)创建任务
  - [ ] 进度查询正常
  - [ ] 部分失败处理正确

### 4. 答题功能

- [ ] **答题流程**
  - [ ] 提交答案成功
  - [ ] 结果反馈正确
  - [ ] 统计数据准确
  - [ ] 错题集正常

### 5. 数据导入导出

- [ ] **导出功能**
  - [ ] 单题库导出成功
  - [ ] 全量导出成功
  - [ ] JSON格式正确

- [ ] **导入功能**
  - [ ] 导入数据成功
  - [ ] 冲突处理正确
  - [ ] 数据验证正常

## 📊 性能测试

### 1. 基准测试

- [ ] **响应时间**
  - [ ] 首页加载 < 2秒
  - [ ] API响应 < 500ms
  - [ ] AI生成进度查询 < 200ms

- [ ] **并发测试**
  - [ ] 10并发用户正常
  - [ ] 50并发用户正常
  - [ ] 数据库连接池充足

### 2. 压力测试

- [ ] **负载测试**
  - [ ] 100 QPS无错误
  - [ ] 内存使用稳定
  - [ ] CPU使用合理

- [ ] **长时间运行**
  - [ ] 24小时稳定运行
  - [ ] 无内存泄漏
  - [ ] 日志轮转正常

## 🔐 安全测试

### 1. 认证授权

- [ ] **Token管理**
  - [ ] Token过期机制正常
  - [ ] Token刷新正常(如启用)
  - [ ] 无效Token被拒绝

- [ ] **权限控制**
  - [ ] 用户只能访问自己的数据
  - [ ] 越权访问被拒绝
  - [ ] 资源隔离正确

### 2. 输入验证

- [ ] **参数验证**
  - [ ] SQL注入防护有效
  - [ ] XSS防护有效
  - [ ] CSRF防护有效(如需要)

- [ ] **文件上传**
  - [ ] 文件类型限制
  - [ ] 文件大小限制
  - [ ] 路径遍历防护

### 3. 数据安全

- [ ] **加密存储**
  - [ ] API密钥已加密
  - [ ] 密码使用bcrypt
  - [ ] 敏感数据不记录日志

- [ ] **传输安全**
  - [ ] 强制HTTPS
  - [ ] HTTP重定向到HTTPS
  - [ ] 安全头配置正确

## 📝 监控和日志

### 1. 日志配置

- [ ] **日志级别**
  - [ ] 生产环境使用Warning级别
  - [ ] 关键操作有日志
  - [ ] 错误日志详细

- [ ] **日志轮转**
  - [ ] 日志文件大小限制
  - [ ] 日志保留期限
  - [ ] 磁盘空间监控

### 2. 监控设置

- [ ] **健康检查**
  - [ ] /health端点正常
  - [ ] 数据库健康检查
  - [ ] 外部监控服务(如UptimeRobot)

- [ ] **告警配置**
  - [ ] 服务宕机告警
  - [ ] 错误率告警
  - [ ] 磁盘空间告警

## 🚀 部署流程

### 1. 预部署

- [ ] 备份当前数据
- [ ] 通知用户维护窗口
- [ ] 准备回滚方案

### 2. 部署步骤

```bash
# 1. 备份数据
./scripts/backup.sh

# 2. 拉取最新代码
git pull origin main

# 3. 更新环境变量
cp .env.production .env

# 4. 拉取最新镜像
docker-compose pull

# 5. 停止旧服务
docker-compose down

# 6. 启动新服务
docker-compose up -d

# 7. 执行数据库迁移
docker-compose exec backend dotnet ef database update

# 8. 检查服务状态
docker-compose ps
docker-compose logs -f
```

### 3. 部署后验证

- [ ] 所有容器状态healthy
- [ ] 健康检查通过
- [ ] 功能测试通过
- [ ] 性能测试通过

### 4. 上线

- [ ] 移除维护页面
- [ ] 通知用户服务恢复
- [ ] 监控运行状态

## 🔄 回滚计划

如果部署失败,执行以下步骤:

```bash
# 1. 停止服务
docker-compose down

# 2. 恢复数据
./scripts/restore.sh backups/backup_before_deploy.tar.gz

# 3. 恢复代码
git checkout previous-stable-version

# 4. 重新部署
docker-compose up -d

# 5. 验证服务
./scripts/wait-for-health.sh http://localhost:5000/health
```

## 📞 联系信息

- 技术支持: support@example.com
- 文档: https://docs.example.com
- 问题反馈: https://github.com/your-username/ai-questionbank/issues

---

**部署日期**: ___________

**部署人员**: ___________

**审核人员**: ___________

**签名**: ___________
