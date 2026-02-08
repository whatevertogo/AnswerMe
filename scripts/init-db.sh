#!/bin/bash
# 数据库初始化脚本

set -e

echo "🔄 等待数据库启动..."

until pg_isready -h ${POSTGRES_HOST:-db} -p ${POSTGRES_PORT:-5432} -U ${POSTGRES_USER:-answeruser} -d ${POSTGRES_DB:-answermedb}; do
  echo "⏳ 数据库尚未就绪,等待中..."
  sleep 2
done

echo "✅ 数据库已就绪!"

echo "🔄 应用数据库迁移..."

cd /app

# 应用EF Core迁移
dotnet ef database update --startup-path /app/AnswerMe.API.dll --context AnswerMeDbContext || {
    echo "❌ 迁移失败,尝试创建数据库..."
    dotnet ef database update --startup-path /app/AnswerMe.API.dll --context AnswerMeDbContext
}

echo "✅ 数据库初始化完成!"

echo "🚀 启动应用..."
exec dotnet AnswerMe.API.dll
