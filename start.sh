#!/bin/bash
# AnswerMe 快速启动脚本
# 支持 Docker Compose 全栈启动 或 本地开发模式

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 AnswerMe 启动脚本${NC}"
echo "========================================"

# 解析命令行参数
MODE="${1:-docker}"
case "$MODE" in
    docker|full)
        MODE="docker"
        ;;
    dev|local)
        MODE="dev"
        ;;
    redis-only)
        MODE="redis-only"
        ;;
    *)
        echo -e "${RED}❌ 未知模式: $MODE${NC}"
        echo ""
        echo "用法: $0 [模式]"
        echo ""
        echo "模式选项:"
        echo "  docker (默认)  - 使用 Docker Compose 启动全部服务"
        echo "  dev, local     - 本地开发模式（仅启动 Redis）"
        echo "  redis-only     - 仅启动 Redis 服务"
        exit 1
        ;;
esac

# 检查 .env 文件
if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  未找到 .env 文件，从 .env.example 复制...${NC}"
    cp .env.example .env
    echo -e "${GREEN}✅ 已创建 .env 文件${NC}"
    echo -e "${YELLOW}📝 请编辑 .env 文件后重新运行，重点设置：${NC}"
    echo "   - JWT_SECRET (至少32字符)"
    echo ""
    exit 1
fi

# 检查 Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker 未安装，请先安装 Docker${NC}"
    exit 1
fi

# 检查 Docker Compose
if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo -e "${RED}❌ Docker Compose 未安装，请先安装 Docker Compose${NC}"
    exit 1
fi

# 使用 docker-compose 或 docker compose
DOCKER_COMPOSE="docker-compose"
if ! command -v docker-compose &> /dev/null; then
    DOCKER_COMPOSE="docker compose"
fi

# ========================================
# 模式 1: Docker Compose 全栈启动
# ========================================
if [ "$MODE" = "docker" ]; then
    echo -e "${BLUE}📦 模式: Docker Compose 全栈启动${NC}"
    echo ""

    # 构建 Docker 镜像
    echo -e "${YELLOW}🔨 构建 Docker 镜像...${NC}"
    $DOCKER_COMPOSE build

    # 启动服务
    echo -e "${YELLOW}🚀 启动服务...${NC}"
    $DOCKER_COMPOSE up -d

    # 等待服务启动
    echo -e "${YELLOW}⏳ 等待服务就绪...${NC}"
    sleep 10

    # 检查服务状态
    echo -e "${YELLOW}🔍 服务状态:${NC}"
    $DOCKER_COMPOSE ps

    # 健康检查
    echo ""
    for i in {1..12}; do
        if curl -f http://localhost:5000/health > /dev/null 2>&1; then
            echo -e "${GREEN}✅ Backend API 运行正常!${NC}"
            echo -e "   ${GREEN}访问地址:${NC} http://localhost:5000"
            echo -e "   ${GREEN}健康检查:${NC} http://localhost:5000/health"
            echo -e "   ${GREEN}Swagger:${NC}   http://localhost:5000/swagger"
            break
        fi
        if [ $i -eq 12 ]; then
            echo -e "${YELLOW}⚠️  Backend API 可能尚未完全启动，请稍后访问${NC}"
            echo -e "   查看日志: ${DOCKER_COMPOSE} logs -f backend"
        fi
        sleep 2
    done

    echo ""
    echo -e "${GREEN}🎉 AnswerMe 已启动!${NC}"
    echo ""
    echo -e "${BLUE}常用命令:${NC}"
    echo "  查看日志:   $DOCKER_COMPOSE logs -f"
    echo "  停止服务:   $DOCKER_COMPOSE down"
    echo "  重启服务:   $DOCKER_COMPOSE restart"
    echo "  清理数据:   $DOCKER_COMPOSE down -v"
    echo ""

# ========================================
# 模式 2: 本地开发模式（仅 Redis）
# ========================================
elif [ "$MODE" = "dev" ] || [ "$MODE" = "redis-only" ]; then
    echo -e "${BLUE}💻 模式: 本地开发${NC}"
    echo ""

    # 启动 Redis
    echo -e "${YELLOW}🚀 启动 Redis 服务...${NC}"
    $DOCKER_COMPOSE up -d redis

    # 检查 Redis
    if $DOCKER_COMPOSE ps redis | grep -q "Up"; then
        echo -e "${GREEN}✅ Redis 运行正常!${NC}"
        echo -e "   ${GREEN}端口:${NC} 6379"
    else
        echo -e "${RED}❌ Redis 启动失败${NC}"
        exit 1
    fi

    echo ""
    echo -e "${BLUE}接下来手动启动后端和前端:${${NC}}"
    echo ""
    echo -e "${YELLOW}# 终端 1 - 后端${NC}"
    echo "cd backend"
    echo "dotnet run --project AnswerMe.API"
    echo ""
    echo -e "${YELLOW}# 终端 2 - 前端${NC}"
    echo "cd frontend"
    echo "npm run dev"
    echo ""

    if [ "$MODE" = "redis-only" ]; then
        echo -e "${GREEN}🎉 仅 Redis 模式启动完成!${NC}"
        echo ""
        echo -e "${BLUE}停止 Redis:${NC} $DOCKER_COMPOSE stop redis"
    fi
fi

echo ""
