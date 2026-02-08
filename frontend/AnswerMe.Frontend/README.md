# AnswerMe Frontend

基于 Vue 3 + TypeScript + Vite 的现代化前端应用。

## 技术栈

- **框架**: Vue 3.5
- **构建工具**: Vite 7
- **语言**: TypeScript 5.9
- **路由**: Vue Router
- **状态管理**: Pinia
- **UI组件库**: Element Plus
- **HTTP客户端**: Axios

## 项目结构

```
src/
├── api/          # API调用模块
│   └── index.ts  # 登录、登出、用户信息接口
├── assets/       # 静态资源
├── components/   # 通用组件
├── views/        # 页面组件
│   ├── Home.vue  # 首页
│   └── Login.vue # 登录页
├── router/       # 路由配置
│   └── index.ts  # 路由定义
├── stores/       # Pinia状态管理
│   ├── app.ts    # 应用全局状态
│   └── user.ts   # 用户认证状态
├── types/        # TypeScript类型定义
│   └── index.ts  # 通用类型定义
├── utils/        # 工具函数
│   └── request.ts # Axios封装
├── styles/       # 全局样式
│   └── index.scss # 全局样式文件
├── App.vue       # 根组件
├── main.ts       # 应用入口
└── style.css     # 基础样式
```

## 环境变量

项目支持多环境配置:

- `.env` - 默认配置
- `.env.development` - 开发环境
- `.env.production` - 生产环境

### 环境变量说明

```bash
VITE_API_BASE_URL=/api  # API基础路径
```

## 开发指南

### 安装依赖

```bash
npm install
```

### 启动开发服务器

```bash
npm run dev
```

### 构建生产版本

```bash
npm run build
```

### 预览生产构建

```bash
npm run preview
```

## 代码规范

- 使用 Vue 3 Composition API (`<script setup>`)
- TypeScript 严格模式
- 遵循单一职责原则
- 组件命名使用 PascalCase
- 文件命名使用 kebab-case

## 状态管理

### User Store (用户认证)

```typescript
import { useUserStore } from '@/stores/user'

const userStore = useUserStore()
userStore.setToken('xxx')
userStore.setUserInfo({ id: 1, username: 'admin' })
userStore.logout()
```

### App Store (应用状态)

```typescript
import { useAppStore } from '@/stores/app'

const appStore = useAppStore()
appStore.setLoading(true)
appStore.toggleSidebar()
appStore.setTheme('dark')
```

## API调用

```typescript
import { loginApi } from '@/api'

const result = await loginApi({
  username: 'admin',
  password: '123456'
})
```

## 路由配置

当前路由:

- `/` - 首页
- `/login` - 登录页

## Element Plus使用

所有Element Plus图标已全局注册,可直接使用:

```vue
<template>
  <el-icon><User /></el-icon>
  <el-icon><Lock /></el-icon>
</template>
```

## Axios拦截器

### 请求拦截器

自动添加Authorization头:

```typescript
config.headers.Authorization = `Bearer ${token}`
```

### 响应拦截器

统一错误处理,自动跳转登录页(401错误)

## License

MIT
