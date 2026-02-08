# AI题库系统 - 前端

基于 Vue 3 + TypeScript + Vite + Element Plus 的现代化前端应用。

## 技术栈

- **框架**: Vue 3 (Composition API)
- **构建工具**: Vite
- **语言**: TypeScript (严格模式)
- **UI组件库**: Element Plus
- **状态管理**: Pinia
- **路由**: Vue Router 4
- **HTTP客户端**: Axios

## 项目结构

```
src/
├── api/              # API请求封装
│   ├── index.ts      # Axios实例配置
│   └── auth.ts       # 认证相关API
├── assets/           # 静态资源
│   └── styles/       # 全局样式
├── components/       # 公共组件
├── router/           # 路由配置
├── stores/           # Pinia状态管理
│   ├── auth.ts       # 认证状态
│   ├── aiConfig.ts   # AI配置状态
│   └── questionBank.ts # 题库状态
├── types/            # TypeScript类型定义
├── utils/            # 工具函数
├── views/            # 页面组件
│   ├── LoginView.vue
│   ├── RegisterView.vue
│   ├── QuestionBanksView.vue
│   └── ...
├── App.vue           # 根组件
└── main.ts           # 应用入口
```

## 开发指南

### 环境变量配置

复制 `.env.example` 为 `.env` 并配置：

```bash
cp .env.example .env
```

主要环境变量：

```bash
# API配置
VITE_API_BASE_URL=http://localhost:5000
VITE_API_TIMEOUT=10000

# 应用配置
VITE_APP_TITLE=AnswerMe AI题库
VITE_APP_PORT=5173

# 功能开关
VITE_ENABLE_MOCK=false
VITE_ENABLE_DEBUG=true
```

### 安装依赖

```bash
npm install
```

### 启动开发服务器

```bash
npm run dev
```

应用将在 http://localhost:5173 启动

### 构建生产版本

```bash
npm run build
```

构建产物将输出到 `dist/` 目录

### 预览生产构建

```bash
npm run preview
```

### 运行测试

```bash
# 运行测试
npm run test

# 测试UI界面
npm run test:ui

# 测试覆盖率
npm run test:coverage
```

### 代码格式化

```bash
# Prettier格式化
npm run format

# ESLint检查
npm run lint
```

### Docker部署

使用Docker Compose一键部署：

```bash
# 在项目根目录
docker-compose up -d

# 访问应用
open http://localhost:3000
```

单独构建前端Docker镜像：

```bash
docker build -t answerme-frontend ./frontend
```

## 核心功能

### 1. 用户认证
- 登录/注册页面
- JWT Token管理
- 路由守卫保护

### 2. 题库管理
- 题库列表展示
- 题库创建/编辑/删除
- 题目管理

### 3. AI配置
- AI Provider配置
- API Key管理
- 配置测试

### 4. 题目生成
- 生成参数配置
- 实时进度显示
- 结果预览和编辑

### 5. 答题功能
- 交互式答题界面
- 答题进度跟踪
- 结果统计分析

## API代理配置

开发环境下，前端通过Vite代理转发API请求到后端：

```typescript
// vite.config.ts
server: {
  port: 5173,
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true
    }
  }
}
```

## 状态管理

使用Pinia进行全局状态管理：

- `useAuthStore`: 用户认证状态
- `useAIConfigStore`: AI配置状态
- `useQuestionBankStore`: 题库状态

## 路由配置

主要路由：

- `/login` - 登录页
- `/register` - 注册页
- `/question-banks` - 题库列表
- `/question-banks/:id` - 题库详情
- `/ai-config` - AI配置
- `/generate` - 生成题目
- `/quiz/:bankId/:sessionId` - 答题页面
- `/result/:sessionId` - 答题结果

## TypeScript配置

项目启用严格模式，确保类型安全：

```json
{
  "compilerOptions": {
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true
  }
}
```

## 样式规范

- 使用Scoped CSS避免样式污染
- 响应式设计，支持移动端
- 遵循Element Plus设计规范

## 开发注意事项

1. **类型安全**: 所有组件使用TypeScript编写
2. **组件化**: 优先使用Composition API
3. **代码复用**: 提取公共逻辑到composables
4. **性能优化**: 路由懒加载，按需引入Element Plus

## 待完成功能

- [ ] 完善API接口调用
- [ ] 实现题库CRUD功能
- [ ] 实现AI配置管理
- [ ] 实现题目生成流程
- [ ] 实现答题功能
- [ ] 添加单元测试

## 贡献指南

1. Fork项目
2. 创建功能分支
3. 提交更改
4. 发起Pull Request

## 许可证

MIT License
