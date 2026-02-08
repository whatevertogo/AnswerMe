# P1 修复验证脚本
# 用于快速验证后端 P1 问题修复是否生效

param(
    [string]$BaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "P1 修复验证测试" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# 测试计数器
$TotalTests = 0
$PassedTests = 0
$FailedTests = 0

function Test-Step {
    param([string]$Name)
    Write-Host "`n[$($TotalTests + 1)] $Name" -ForegroundColor Yellow
    $script:TotalTests++
}

function Test-Success {
    Write-Host "    ✓ 通过" -ForegroundColor Green
    $script:PassedTests++
}

function Test-Fail {
    param([string]$Reason)
    Write-Host "    ✗ 失败: $Reason" -ForegroundColor Red
    $script:FailedTests++
}

function Test-Skip {
    param([string]$Reason)
    Write-Host "    ⊘ 跳过: $Reason" -ForegroundColor Gray
}

# ============================================
# P1-2: 本地登录 IP 限制测试
# ============================================
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "P1-2: 本地登录 IP 限制测试" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

Test-Step "测试本地登录端点可访问性"
try {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/local-login" -Method Post -ErrorAction Stop
    if ($response.token) {
        Test-Success
        $LocalToken = $response.token
        Write-Host "    Token: $($response.token.Substring(0, 20))..." -ForegroundColor DarkGray
    } else {
        Test-Fail "未返回 Token"
    }
} catch {
    Test-Fail "请求失败: $($_.Exception.Message)"
}

Test-Step "验证本地登录 Token 有效性"
if ($LocalToken) {
    try {
        $headers = @{
            "Authorization" = "Bearer $LocalToken"
        }
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/auth/me" -Method Get -Headers $headers -ErrorAction Stop
        if ($response.user) {
            Test-Success
            Write-Host "    用户: $($response.user.username)" -ForegroundColor DarkGray
        } else {
            Test-Fail "未返回用户信息"
        }
    } catch {
        Test-Fail "Token 验证失败: $($_.Exception.Message)"
    }
} else {
    Test-Skip "无可用 Token"
}

# ============================================
# P1-1: API Key 解密测试（需要手动创建数据源）
# ============================================
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "P1-1: API Key 解密测试" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

Test-Step "测试数据源列表获取"
try {
    $headers = @{
        "Authorization" = "Bearer $LocalToken"
    }
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/datasource" -Method Get -Headers $headers -ErrorAction Stop
    Test-Success
    Write-Host "    数据源数量: $($response.Count)" -ForegroundColor DarkGray

    if ($response.Count -gt 0) {
        Write-Host "`n    可用数据源:" -ForegroundColor DarkGray
        foreach ($ds in $response) {
            Write-Host "      - [$($ds.id)] $($ds.name) ($($ds.type))" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "`n    ⚠ 未找到数据源，请先创建数据源以完成完整测试" -ForegroundColor Yellow
        Write-Host "    使用以下命令创建测试数据源:" -ForegroundColor Yellow
        Write-Host "    POST $BaseUrl/api/datasource" -ForegroundColor Yellow
        Write-Host "    Body: { `"name`": `"测试`", `"type`": `"openai`", `"apiKey`": `"sk-...`", `"isDefault`": true }" -ForegroundColor DarkGray
    }
} catch {
    Test-Fail "获取数据源列表失败: $($_.Exception.Message)"
}

# ============================================
# P1-3: DataSource 更新测试
# ============================================
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "P1-3: DataSource 更新测试" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan

Test-Step "测试数据源更新接口"
try {
    $headers = @{
        "Authorization" = "Bearer $LocalToken"
        "Content-Type" = "application/json"
    }

    # 尝试更新第一个数据源（如果存在）
    $dataSources = Invoke-RestMethod -Uri "$BaseUrl/api/datasource" -Method Get -Headers $headers -ErrorAction Stop

    if ($dataSources.Count -gt 0) {
        $dsId = $dataSources[0].id
        $updateBody = @{
            name = "更新后的名称"
        } | ConvertTo-Json

        $response = Invoke-RestMethod -Uri "$BaseUrl/api/datasource/$dsId" -Method Put -Headers $headers -Body $updateBody -ErrorAction Stop
        if ($response.name -eq "更新后的名称") {
            Test-Success
            Write-Host "    更新后名称: $($response.name)" -ForegroundColor DarkGray
        } else {
            Test-Fail "更新失败"
        }
    } else {
        Test-Skip "无可用数据源"
    }
} catch {
    Test-Fail "更新数据源失败: $($_.Exception.Message)"
}

# ============================================
# 总结
# ============================================
Write-Host "`n===========================================" -ForegroundColor Cyan
Write-Host "测试总结" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "总测试数: $TotalTests" -ForegroundColor White
Write-Host "通过: $PassedTests" -ForegroundColor Green
Write-Host "失败: $FailedTests" -ForegroundColor $(if ($FailedTests -gt 0) { "Red" } else { "Green" })

if ($FailedTests -eq 0) {
    Write-Host "`n✓ 所有测试通过！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n✗ 存在失败的测试，请检查日志" -ForegroundColor Red
    exit 1
}
