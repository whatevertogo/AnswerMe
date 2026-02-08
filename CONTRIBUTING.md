# 贡献指南

感谢您考虑为AI Question Bank做出贡献!本文档将指导您如何参与项目开发。

## 目录

- [行为准则](#行为准则)
- [如何贡献](#如何贡献)
- [开发环境设置](#开发环境设置)
- [代码规范](#代码规范)
- [提交规范](#提交规范)
- [Pull Request流程](#pull-request流程)
- [问题反馈](#问题反馈)

## 行为准则

### 我们的承诺

为了营造开放和友好的环境,我们承诺让每个人都能参与项目,无论经验水平、性别、性别认同和表达、性取向、残疾、个人外貌、体型、种族、民族、年龄、宗教或国籍。

### 我们的标准

积极行为包括:
- 使用友好和包容的语言
- 尊重不同的观点和经验
- 优雅地接受建设性批评
- 关注对社区最有利的事情
- 对其他社区成员表示同理心

不可接受的行为包括:
- 使用性化的语言或图像,以及不受欢迎的性关注或勾引
- 恶意攻击、侮辱/贬损的评论,以及人身或政治攻击
- 公开或私下骚扰
- 未经明确许可发布他人的私人信息
- 其他不道德或不专业的行为

## 如何贡献

### 贡献方式

1. **报告Bug** - 发现问题请创建Issue
2. **讨论功能** - 在Discussions中讨论新功能想法
3. **提交代码** - 修复Bug或实现新功能
4. **改进文档** - 完善文档和示例
5. **帮助他人** - 回答Issue中的问题

### 开始之前

- 检查[现有Issues](https://github.com/your-username/ai-questionbank/issues)避免重复
- 查看[文档](docs/)了解项目架构
- 如果是重大改动,先创建Discussion讨论

## 开发环境设置

### 前置要求

- .NET 8 SDK
- Node.js 18+
- Docker & Docker Compose
- Git

### 克隆仓库

```bash
# Fork仓库到您的GitHub账号
# 然后克隆您的fork
git clone https://github.com/your-username/ai-questionbank.git
cd ai-questionbank

# 添加上游仓库
git remote add upstream https://github.com/original-owner/ai-questionbank.git
```

### 后端开发

```bash
# 进入后端目录
cd backend

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行项目(开发模式)
dotnet run

# 运行测试
dotnet test

# 运行并监听文件变化
dotnet watch run
```

### 前端开发

```bash
# 进入前端目录
cd frontend

# 安装依赖
npm install

# 运行开发服务器
npm run dev

# 构建生产版本
npm run build

# 运行测试
npm run test

# 代码检查
npm run lint
```

### Docker开发

```bash
# 使用Docker Compose启动开发环境
docker-compose -f docker-compose.dev.yml up -d

# 查看日志
docker-compose logs -f

# 进入容器调试
docker-compose exec backend bash
docker-compose exec frontend sh
```

## 代码规范

### C# 代码规范

遵循 [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// ✅ 好的命名
public class UserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetUserByIdAsync(string userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}

// ❌ 不好的命名
public class userservice
{
    private ApplicationDbContext db;

    public userservice(ApplicationDbContext db)
    {
        this.db = db;
    }

    public User getuser(string id)
    {
        return db.Users.FirstOrDefault(u => u.Id == id);
    }
}
```

**规则**:
- 使用PascalCase命名类、方法、属性
- 使用camelCase命名局部变量、参数
- 私有字段使用_underscore前缀
- 异步方法使用Async后缀
- 接口使用I前缀

### TypeScript/Vue 代码规范

```typescript
// ✅ 好的代码
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export const useUserStore = defineStore('user', () => {
  const user = ref<User | null>(null);
  const isLoggedIn = computed(() => user.value !== null);

  async function login(email: string, password: string) {
    // ...
  }

  return { user, isLoggedIn, login };
});

// ❌ 不好的代码
import { ref } from 'vue';

const user = ref(null);
const loggedIn = ref(false);

async function doLogin(email, password) {
  // ...
}
```

**规则**:
- 使用Composition API
- 使用TypeScript类型
- 使用Setup语法
- 组件使用PascalCase
- 函数和变量使用camelCase

### 文件组织

```
src/
├── Components/          # 可复用组件
│   ├── Button.vue
│   └── Input.vue
├── Views/              # 页面组件
│   ├── Home.vue
│   └── Login.vue
├── Stores/             # Pinia状态
│   ├── user.ts
│   └── questions.ts
├── Services/           # API服务
│   ├── api.ts
│   └── auth.ts
├── Types/              # TypeScript类型
│   └── index.ts
└── Utils/              # 工具函数
    └── helpers.ts
```

### 注释规范

```csharp
/// <summary>
/// 用户服务
/// </summary>
public class UserService
{
    /// <summary>
    /// 根据用户ID获取用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户对象或null</returns>
    public async Task<User?> GetUserByIdAsync(string userId)
    {
        // 复杂逻辑需要注释
        var user = await _context.Users
            .Include(u => u.AIConfigs)  // 预加载AI配置
            .FirstOrDefaultAsync(u => u.Id == userId);

        return user;
    }
}
```

**原则**:
- 公共API需要XML文档注释
- 复杂逻辑需要解释为什么
- 不需要注释显而易见的代码

## 提交规范

### Commit Message格式

遵循 [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type类型

- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档更新
- `style`: 代码格式(不影响功能)
- `refactor`: 重构(既不是新功能也不是修复)
- `perf`: 性能优化
- `test`: 添加测试
- `chore`: 构建过程或辅助工具变动
- `ci`: CI配置变动

### 示例

```bash
# 新功能
git commit -m "feat(ai): 添加通义千问Provider支持"

# Bug修复
git commit -m "fix(auth): 修复JWT token过期时间计算错误"

# 文档
git commit -m "docs(readme): 更新安装指南"

# 重构
git commit -m "refactor(questions): 优化题目查询性能"

# 破坏性变更
git commit -m "feat(api)!: 移除旧的题目列表API

BREAKING CHANGE: 使用新的分页API替代旧的offset分页"
```

## Pull Request流程

### 1. 创建分支

```bash
# 从main创建功能分支
git checkout main
git pull upstream main
git checkout -b feat/your-feature-name

# 或修复分支
git checkout -b fix/bug-description
```

### 2. 开发和提交

```bash
# 查看改动
git status
git diff

# 提交改动
git add .
git commit -m "feat: add new feature"

# 推送到您的fork
git push origin feat/your-feature-name
```

### 3. 创建Pull Request

在GitHub上创建Pull Request,标题格式:

```
feat(ai): 添加通义千问Provider支持

fix(auth): 修复JWT token过期时间计算错误

docs(readme): 更新安装指南
```

### PR描述模板

```markdown
## 变更类型
- [ ] Bug修复
- [ ] 新功能
- [ ] 破坏性变更
- [ ] 文档更新

## 描述
简要描述此PR的改动内容和原因。

## 相关Issue
Fixes #123
Related to #456

## 变更内容
- 添加了通义千问Provider
- 实现了AI Provider抽象层
- 添加了单元测试

## 测试
- [ ] 单元测试通过
- [ ] 集成测试通过
- [ ] 手动测试通过

## 截图(如有)
![screenshot](https://example.com/screenshot.png)

## Checklist
- [ ] 代码遵循项目规范
- [ ] 已添加必要的文档
- [ ] 已添加测试
- [ ] 所有测试通过
- [ ] 无新的警告
```

### 4. 代码审查

维护者会审查您的代码并提供反馈。请:

- 及时回应评论
- 感谢审查者的时间
- 解释不清楚的地方
- 按要求修改代码

### 5. 合并

审查通过后,您的PR将被合并到main分支。

### 分支命名规范

- `feat/feature-name`: 新功能
- `fix/bug-description`: Bug修复
- `docs/doc-update`: 文档更新
- `refactor/code-improvement`: 重构
- `test/add-tests`: 添加测试
- `chore/update-deps`: 依赖更新

## 问题反馈

### Bug报告

使用Issue模板报告Bug:

```markdown
**Bug描述**
清晰简洁地描述Bug是什么。

**复现步骤**
1. 访问 '...'
2. 点击 '....'
3. 滚动到 '....'
4. 看到错误

**预期行为**
描述您期望发生什么。

**截图**
如果适用,添加截图来说明问题。

**环境信息**
- OS: [e.g. Windows 11, Ubuntu 22.04]
- Browser: [e.g. Chrome 120, Firefox 121]
- Version: [e.g. v0.1.0-alpha]

**附加信息**
添加其他有助于理解问题的信息。
```

### 功能请求

```markdown
**功能描述**
您想要的功能是什么?

**问题背景**
这个功能解决什么问题?

**解决方案**
您希望如何实现这个功能?

**替代方案**
是否考虑过其他解决方案?

**附加信息**
添加其他相关信息或截图。
```

## 开发最佳实践

### 1. 保持PR小而专注

- ✅ 一个PR解决一个问题
- ✅ PR代码量控制在400行以内
- ❌ 不要在PR中重构无关代码
- ❌ 不要在PR中依赖多个大型改动

### 2. 编写测试

```csharp
[Fact]
public async Task GetUserByIdAsync_WhenUserExists_ReturnsUser()
{
    // Arrange
    var userId = "test-user-id";
    var expectedUser = new User { Id = userId, Email = "test@example.com" };
    _context.Users.Add(expectedUser);
    await _context.SaveChangesAsync();

    // Act
    var result = await _service.GetUserByIdAsync(userId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(userId, result.Id);
}
```

### 3. 编写文档

- 公共API需要XML注释
- 复杂逻辑需要解释
- README更新新功能说明

### 4. 性能考虑

- 避免N+1查询
- 使用异步方法
- 考虑缓存策略
- 添加性能测试

### 5. 安全考虑

- 验证所有输入
- 不信任客户端数据
- 加密敏感信息
- 遵循OWASP Top 10

## 获取帮助

- 💬 [Discussions](https://github.com/your-username/ai-questionbank/discussions) - 技术讨论
- 🐛 [Issues](https://github.com/your-username/ai-questionbank/issues) - Bug报告
- 📧 Email: maintainers@example.com
- 💬 Discord: [加入社区](https://discord.gg/your-server)

## 认可贡献者

我们会在以下地方认可贡献者:

- README.md中的贡献者列表
- Release Notes中的致谢
- 网站上的贡献者页面

## 许可证

贡献的代码将采用项目的[MIT License](LICENSE)。

---

再次感谢您的贡献!🎉
