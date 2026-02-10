# AnswerMe 快速启动脚本 (PowerShell)
# 支持 Docker Compose 全栈启动 或 本地开发模式

param(
    [Parameter(Position=0)]
    [ValidateSet('docker', 'dev', 'redis-only', 'full')]
    [string]$Mode = 'docker'
)

# 颜色函数
function Write-Color {
    param([string]$Message, [string]$Color = 'White')
    Write-Host $Message -ForegroundColor $Color
}

Write-Color '🚀 AnswerMe 启动脚本' Cyan
Write-Host '========================================' -ForegroundColor Gray
Write-Host ""

# 检查 .env 文件
if (-not (Test-Path .env)) {
    Write-Color '⚠️  未找到 .env 文件，从 .env.example 复制...' Yellow
    Copy-Item .env.example .env
    Write-Color '✅ 已创建 .env 文件' Green
    Write-Color '📝 请编辑 .env 文件后重新运行，重点设置：' Yellow
    Write-Host '   - JWT_SECRET (至少32字符)' -ForegroundColor Gray
    Write-Host ''
    exit 1
}

# 检查 Docker
$dockerExists = Get-Command docker -ErrorAction SilentlyContinue
if (-not $dockerExists) {
    Write-Color '❌ Docker 未安装，请先安装 Docker' Red
    exit 1
}

# 检查 Docker Compose
$dockerComposeExists = Get-Command docker-compose -ErrorAction SilentlyContinue
$dockerComposePlugin = docker compose version 2>&1
if (-not $dockerComposeExists -and $LASTEXITCODE -ne 0) {
    Write-Color '❌ Docker Compose 未安装，请先安装 Docker Compose' Red
    exit 1
}

# 确定使用的命令
$dockerCompose = 'docker-compose'
if (-not $dockerComposeExists) {
    $dockerCompose = 'docker compose'
}

# ========================================
# 模式 1: Docker Compose 全栈启动
# ========================================
if ($Mode -eq 'docker' -or $Mode -eq 'full') {
    Write-Color '📦 模式: Docker Compose 全栈启动' Cyan
    Write-Host ''

    # 构建 Docker 镜像
    Write-Color '🔨 构建 Docker 镜像...' Yellow
    Invoke-Expression "$dockerCompose build"
    if ($LASTEXITCODE -ne 0) {
        Write-Color '❌ 构建失败' Red
        exit 1
    }

    # 启动服务
    Write-Color '🚀 启动服务...' Yellow
    Invoke-Expression "$dockerCompose up -d"
    if ($LASTEXITCODE -ne 0) {
        Write-Color '❌ 启动失败' Red
        exit 1
    }

    # 等待服务启动
    Write-Color '⏳ 等待服务就绪...' Yellow
    Start-Sleep -Seconds 10

    # 检查服务状态
    Write-Color '🔍 服务状态:' Yellow
    Invoke-Expression "$dockerCompose ps"

    # 健康检查
    Write-Host ''
    $healthy = $false
    for ($i = 1; $i -le 12; $i++) {
        try {
            $response = Invoke-WebRequest -Uri 'http://localhost:5000/health' -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Color '✅ Backend API 运行正常!' Green
                Write-Host '   访问地址: http://localhost:5000' -ForegroundColor Green
                Write-Host '   健康检查: http://localhost:5000/health' -ForegroundColor Green
                Write-Host '   Swagger:   http://localhost:5000/swagger' -ForegroundColor Green
                $healthy = $true
                break
            }
        } catch {
            # 继续重试
        }
        Start-Sleep -Seconds 2
    }

    if (-not $healthy) {
        Write-Color '⚠️  Backend API 可能尚未完全启动，请稍后访问' Yellow
        Write-Host "   查看日志: $dockerCompose logs -f backend" -ForegroundColor Gray
    }

    Write-Host ''
    Write-Color '🎉 AnswerMe 已启动!' Green
    Write-Host ''
    Write-Color '常用命令:' Cyan
    Write-Host "  查看日志:   $dockerCompose logs -f" -ForegroundColor Gray
    Write-Host "  停止服务:   $dockerCompose down" -ForegroundColor Gray
    Write-Host "  重启服务:   $dockerCompose restart" -ForegroundColor Gray
    Write-Host "  清理数据:   $dockerCompose down -v" -ForegroundColor Gray
    Write-Host ''
}

# ========================================
# 模式 2: 本地开发模式（仅 Redis）
# ========================================
elseif ($Mode -eq 'dev' -or $Mode -eq 'redis-only') {
    Write-Color '💻 模式: 本地开发' Cyan
    Write-Host ''

    # 启动 Redis
    Write-Color '🚀 启动 Redis 服务...' Yellow
    Invoke-Expression "$dockerCompose up -d redis"
    if ($LASTEXITCODE -ne 0) {
        Write-Color '❌ Redis 启动失败' Red
        exit 1
    }

    # 检查 Redis 状态
    $redisStatus = Invoke-Expression "$dockerCompose ps redis"
    if ($redisStatus -match 'Up') {
        Write-Color '✅ Redis 运行正常!' Green
        Write-Host '   端口: 6379' -ForegroundColor Green
    } else {
        Write-Color '❌ Redis 启动失败' Red
        exit 1
    }

    Write-Host ''
    Write-Color '接下来手动启动后端和前端:' Cyan
    Write-Host ''
    Write-Color '# 终端 1 - 后端' Yellow
    Write-Host 'cd backend' -ForegroundColor Gray
    Write-Host 'dotnet run --project AnswerMe.API' -ForegroundColor Gray
    Write-Host ''
    Write-Color '# 终端 2 - 前端' Yellow
    Write-Host 'cd frontend' -ForegroundColor Gray
    Write-Host 'npm run dev' -ForegroundColor Gray
    Write-Host ''

    if ($Mode -eq 'redis-only') {
        Write-Color '🎉 仅 Redis 模式启动完成!' Green
        Write-Host ''
        Write-Host "停止 Redis: $dockerCompose stop redis" -ForegroundColor Gray
    }
}

Write-Host ''
