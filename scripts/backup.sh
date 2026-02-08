#!/bin/bash
# AI Question Bank - 数据备份脚本
# 用法: ./backup.sh

set -e

# ==========================================
# 配置
# ==========================================

BACKUP_DIR="${BACKUP_DIR:-./backups}"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=${RETENTION_DAYS:-7}

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# ==========================================
# 函数
# ==========================================

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
# 开始备份
# ==========================================

log_info "开始备份..."

# 创建备份目录
mkdir -p "$BACKUP_DIR"

# ==========================================
# 备份数据库
# ==========================================

log_info "备份数据库..."
docker-compose exec -T db pg_dump -U postgres questionbank > "$BACKUP_DIR/db_$DATE.sql"

if [ $? -eq 0 ]; then
    log_info "数据库备份成功: db_$DATE.sql"
else
    log_error "数据库备份失败"
    exit 1
fi

# ==========================================
# 备份环境配置
# ==========================================

log_info "备份环境配置..."
if [ -f .env ]; then
    cp .env "$BACKUP_DIR/.env_$DATE"
    log_info "环境配置备份成功: .env_$DATE"
else
    log_warn ".env文件不存在,跳过"
fi

# ==========================================
# 压缩备份
# ==========================================

log_info "压缩备份文件..."
tar czf "$BACKUP_DIR/backup_$DATE.tar.gz" -C "$BACKUP_DIR" "db_$DATE.sql" ".env_$DATE" 2>/dev/null || true

# 删除临时文件
rm -f "$BACKUP_DIR/db_$DATE.sql" "$BACKUP_DIR/.env_$DATE"

log_info "备份压缩成功: backup_$DATE.tar.gz"

# ==========================================
# 清理旧备份
# ==========================================

log_info "清理 ${RETENTION_DAYS} 天前的备份..."
find "$BACKUP_DIR" -name "backup_*.tar.gz" -mtime +$RETENTION_DAYS -delete

log_info "清理完成"

# ==========================================
# 备份完成
# ==========================================

# 计算备份文件大小
BACKUP_SIZE=$(du -h "$BACKUP_DIR/backup_$DATE.tar.gz" | cut -f1)

log_info "备份完成!"
echo "备份文件: $BACKUP_DIR/backup_$DATE.tar.gz"
echo "备份大小: $BACKUP_SIZE"
echo "保留天数: $RETENTION_DAYS 天"

exit 0
