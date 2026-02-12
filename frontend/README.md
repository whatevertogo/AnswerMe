# AnswerMe Frontend

自托管智能题库系统前端。

## 技术栈

- Vue 3, TypeScript, Element Plus, Pinia, Tailwind CSS v3.x

## 架构

```
frontend/src/
├── api/           # API 请求函数
├── stores/        # Pinia 状态管理
├── types/         # TypeScript 类型定义
├── views/         # 页面组件
├── components/    # 可复用组件
├── composables/   # 组合式函数
├── router/        # 路由配置
└── utils/         # 工具函数
```

## 启动

```bash
cd frontend && npm install && npm run dev
```

## 代码规范

- API: `import { request }` 命名导入
- Element Plus v3+: `el-radio`/`el-checkbox` 用 `value` 非 `label`
- 深色模式: 用 `styles/theme.css` CSS 变量，禁止硬编码颜色

## 数据模型

Question 数据使用 `types/question.ts` 类型定义：
- `choice`, `boolean`, `fillBlank`, `shortAnswer`
