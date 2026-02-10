# 多阶段构建 - .NET 10 Backend API

# 阶段1: 构建
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 复制csproj文件
COPY ["AnswerMe.Domain/AnswerMe.Domain.csproj", "AnswerMe.Domain/"]
COPY ["AnswerMe.Application/AnswerMe.Application.csproj", "AnswerMe.Application/"]
COPY ["AnswerMe.Infrastructure/AnswerMe.Infrastructure.csproj", "AnswerMe.Infrastructure/"]
COPY ["AnswerMe.API/AnswerMe.API.csproj", "AnswerMe.API/"]

# 还原依赖
RUN dotnet restore "AnswerMe.API/AnswerMe.API.csproj"

# 复制所有源代码
COPY . .

# 构建项目
WORKDIR "/src/AnswerMe.API"
RUN dotnet build "AnswerMe.API.csproj" -c Release -o /app/build

# 阶段2: 发布
FROM build AS publish
RUN dotnet publish "AnswerMe.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 阶段3: 运行
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# 安装curl用于健康检查
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# ✅ 创建Data Protection密钥目录（非root用户无法创建）
RUN mkdir -p /app/keys

# ✅ 安全改进: 创建非root用户
RUN groupadd -r appuser && useradd -r -g appuser -u 1000 appuser

# 复制发布的应用
COPY --from=publish /app/publish .

# ✅ 安全改进: 设置应用目录权限
RUN chown -R appuser:appuser /app

# 切换到非root用户
USER appuser

# 暴露端口
EXPOSE 8080

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://127.0.0.1:8080/health || exit 1

ENTRYPOINT ["dotnet", "AnswerMe.API.dll"]
