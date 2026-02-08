#!/bin/bash
# AI Question Bank - 数据恢复脚本
# 用法: ./restore.sh <backup_file>

set -e

# ==========================================
# 检查参数
# ==========================================

if [ $# -eq 0 ]; then
    echo "错误: 请提供备份文件"
    echo "用法: $0 <backup_file.tar.gz>"
    exit 1
fi

BACKUP_FILE=$1

if [ ! -f "$BACKUP_FILE" ]; then
    echo "错误: 备份文件不存在: $BACKUP_FILE"
    exit 1
fi

# ==========================================
# 配置
# ==========================================

TEMP_DIR="./restore_temp"
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# ==========================================
# 确认恢复操作
# ==========================================

log_warn "警告: 此操作将覆盖当前数据!"
read -p "确定要恢复吗? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    log_info "恢复操作已取消"
    exit 0
fi

# ==========================================
# 停止服务
# ==========================================

log_info "停止服务..."
docker-compose down

# ==========================================
# 解压备份文件
# ==========================================

log_info "解压备份文件..."
mkdir -p "$TEMP_DIR"
tar xzf "$BACKUP_FILE" -C "$TEMP_DIR"

# 查找解压后的文件
DB_FILE=$(find "$TEMP_DIR" -name "db_*.sql" | head -n 1)
ENV_FILE=$(find "$TEMP_DIR" -name ".env_*" | head -n 1)

if [ -z "$DB_FILE" ]; then
    log_error "未找到数据库备份文件"
    exit 1
fi

# ==========================================
# 启动数据库
# ==========================================

log_info "启动数据库..."
docker-compose up -d db

# 等待数据库就绪
log_info "等待数据库启动..."
./scripts/wait-for-health.sh http://localhost:5432 30 2 || true

sleep 5

# ==========================================
# 恢复数据库
# ==========================================

log_info "恢复数据库..."
docker-compose exec -T db psql -U postgres questionbank < "$DB_FILE"

if [ $? -eq 0 ]; then
    log_info "数据库恢复成功"
else
    log_error "数据库恢复失败"
    docker-compose down
    exit 1
fi

# ==========================================
# 恢复环境配置
# ==========================================

if [ -n "$ENV_FILE" ]; then
    log_info "恢复环境配置..."
    cp "$ENV_FILE" .env
    log_info "环境配置恢复成功"
else
    log_warn "未找到环境配置备份文件"
fi

# ==========================================
# 启动所有服务
# ==========================================

log_info "启动所有服务..."
docker-compose up -d

# ==========================================
# 等待服务就绪
# ==========================================

log_info "等待服务启动..."
sleep 10

# 检查后端健康状态
log_info "检查后端服务..."
./scripts/wait-for-health.sh http://localhost:5000/health 30 2

# 检查前端健康状态
log_info "检查前端服务..."
./scripts/wait-for-health.sh http://localhost:3000/health 30 2

# ==========================================
# 清理临时文件
# ==========================================

log_info "清理临时文件..."
rm -rf "$TEMP_DIR"

# ==========================================
# 恢复完成
# ==========================================

log_info "恢复完成!"
echo "备份文件: $BACKUP_FILE"
echo ""
echo "服务状态:"
docker-compose ps

exit 0
