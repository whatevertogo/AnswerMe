# 集成测试示例

本目录包含 AnswerMe 项目的集成测试示例。

## 集成测试类型

### 1. API 集成测试
测试 HTTP 端点的请求/响应行为。

**示例结构：**
```csharp
public class AuthApiTests
{
    [Fact]
    public async Task LocalLogin_ReturnsSuccess()
    {
        // Arrange
        await using var application = new WebApplicationFactory<Program>();
        var client = application.CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/local-login", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result?.Token);
    }
}
```

**前提条件：**
- `Program.cs` 需要声明 `public partial class Program`
- 测试项目需要引用 `Microsoft.AspNetCore.Mvc.Testing`

### 2. 服务层测试
使用 Moq 模拟仓储依赖，测试应用服务逻辑。

**示例结构：**
```csharp
public class QuestionBankServiceTests
{
    [Fact]
    public async Task CreateQuestionBank_ValidInput_ReturnsDto()
    {
        // Arrange
        var mockRepo = new Mock<IQuestionBankRepository>();
        var service = new QuestionBankService(mockRepo.Object);

        // Act
        var result = await service.CreateQuestionBankAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("题库名称", result.Name);
    }
}
```

### 3. 数据库集成测试
使用真实数据库（SQLite 或 PostgreSQL）测试完整的 CRUD 操作。

**示例结构：**
```csharp
public class QuestionBankRepositoryTests : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task AddQuestionBank_SavesToDatabase()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repo = new QuestionBankRepository(context);

        // Act
        var entity = new QuestionBank { Name = "测试" };
        await repo.AddAsync(entity);
        await context.SaveChangesAsync();

        // Assert
        var saved = await context.QuestionBanks.FindAsync(entity.Id);
        Assert.NotNull(saved);
    }
}
```

## 运行集成测试

```bash
# 运行所有集成测试
dotnet test --filter "FullyQualifiedName~Integration"

# 运行特定测试类
dotnet test --filter "FullyQualifiedName~AuthApiTests"

# 运行特定测试方法
dotnet test --filter "FullyQualifiedName~LocalLogin_ReturnsSuccess"
```

## 测试数据管理

- **开发环境**：使用 SQLite 内存数据库（快速、隔离）
- **CI 环境**：使用 PostgreSQL（与生产环境一致）
- **测试数据**：使用 AutoFixture 生成测试数据
- **清理策略**：每个测试后自动回滚事务

## 依赖项

```xml
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="AutoFixture.Xunit2" Version="4.18.1" />
```

## 最佳实践

1. **测试隔离**：每个测试独立运行，不依赖其他测试
2. **异步测试**：所有异步方法使用 `async/await`
3. **清晰的断言**：使用 FluentAssertions 提高可读性
4. **测试命名**：使用 `MethodName_ExpectedBehavior_StateUnderTest` 格式
5. **避免测试实现细节**：测试行为而非实现

## 相关文档

- [xUnit 文档](https://xunit.net/)
- [Moq 文档](https://github.com/moq/moq4)
- [AutoFixture 文档](https://github.com/AutoFixture/AutoFixture)
- [EF Core 测试策略](https://docs.microsoft.com/ef/core/testing/)
