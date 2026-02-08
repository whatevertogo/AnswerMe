#!/bin/bash
# 等待服务健康检查通过
# 用法: ./wait-for-health.sh http://localhost:5000/health

set -e

# 颜色定义
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 检查参数
if [ $# -eq 0 ]; then
    echo -e "${RED}错误: 请提供健康检查URL${NC}"
    echo "用法: $0 <url> [max_attempts] [interval]"
    exit 1
fi

HEALTH_URL=$1
MAX_ATTEMPTS=${2:-30}     # 默认最多尝试30次
INTERVAL=${3:-2}          # 默认每次间隔2秒
TIMEOUT=${4:-5}           # 默认请求超时5秒

echo -e "${YELLOW}等待服务启动...${NC}"
echo "健康检查URL: $HEALTH_URL"
echo "最大尝试次数: $MAX_ATTEMPTS"
echo "检查间隔: ${INTERVAL}秒"

attempt=1
while [ $attempt -le $MAX_ATTEMPTS ]; do
    echo -n "第 $attempt 次尝试... "

    # 使用curl进行健康检查
    if curl -s -f -m $TIMEOUT "$HEALTH_URL" > /dev/null 2>&1; then
        echo -e "${GREEN}成功!${NC}"
        echo -e "${GREEN}服务已就绪${NC}"
        exit 0
    fi

    echo -e "${YELLOW}失败${NC}"

    if [ $attempt -lt $MAX_ATTEMPTS ]; then
        sleep $INTERVAL
    fi

    attempt=$((attempt + 1))
done

echo -e "${RED}超时错误: 服务在 ${MAX_ATTEMPTS} 次尝试后仍未就绪${NC}"
exit 1
