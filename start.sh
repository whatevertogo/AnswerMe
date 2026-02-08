#!/bin/bash
# AnswerMe 快速启动脚本

set -e

echo "🚀 AnswerMe 启动中..."

# 检查.env文件
if [ ! -f .env ]; then
    echo "⚠️  未找到.env文件,从.env.example复制..."
    cp .env.example .env
    echo "✅ 已创建.env文件,请编辑其中的配置后重新运行!"
    echo "📝 重要: 请修改JWT_SECRET和POSTGRES_PASSWORD!"
    exit 1
fi

# 检查Docker
if ! command -v docker &> /dev/null; then
    echo "❌ Docker未安装,请先安装Docker"
    exit 1
fi

if ! command -v docker-compose &> /dev/null; then
    echo "❌ Docker Compose未安装,请先安装Docker Compose"
    exit 1
fi

# 构建并启动
echo "🔨 构建Docker镜像..."
docker-compose build

echo "🚀 启动服务..."
docker-compose up -d

echo "⏳ 等待服务启动..."
sleep 10

# 检查服务状态
echo "🔍 检查服务状态..."
docker-compose ps

# 检查健康状态
if curl -f http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ Backend API 运行正常!"
    echo "   访问地址: http://localhost:5000"
    echo "   健康检查: http://localhost:5000/health"
else
    echo "⚠️  Backend API 可能尚未完全启动,请稍后访问"
    echo "   查看日志: docker-compose logs -f backend"
fi

echo ""
echo "🎉 AnswerMe 已启动!"
echo ""
echo "常用命令:"
echo "  查看日志: docker-compose logs -f"
echo "  停止服务: docker-compose down"
echo "  重启服务: docker-compose restart"
echo "  清理数据: docker-compose down -v"
echo ""
