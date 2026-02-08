# AnswerMe 前端设计系统

## 🎨 配色方案 (Solarized Light)

```css
/* 主色调 */
--color-primary: #268BD2        /* 蓝色 - 主按钮、链接 */
--color-primary-hover: #1F78B4   /* 深蓝 - 悬停状态 */
--color-secondary: #2AA198       /* 青色 - 辅助元素 */
--color-accent: #CB4B16          /* 橙色 - 强调 */

/* 背景色 */
--color-bg: #FDF6E3              /* 米色 - 主背景 */
--color-bg-secondary: #EEE8D5    /* 浅米色 - 次要背景 */
--color-bg-tertiary: #E8E4CE     /* 边框色 */
--color-white: #FFFFFF            /* 卡片背景 */

/* 文字颜色 */
--color-text-primary: #073642    /* 深蓝灰 - 标题 */
--color-text-secondary: #586E75  /* 中灰 - 正文 */
--color-text-muted: #657B83      /* 浅灰 - 辅助文字 */
--color-text-light: #839496       /* 极浅灰 - 占位符 */

/* 功能色 */
--color-success: #859900         /* 绿色 */
--color-warning: #B58900          /* 黄色 */
--color-danger: #DC322F           /* 红色 */
```

## 📐 设计原则

### 1. 完全响应式
- ❌ 禁止使用固定 `max-width` 限制容器
- ✅ 使用百分比、fr、minmax() 等 CSS 单位
- ✅ 添加响应式断点 (`@media`)

### 2. 网页端优先
- ✅ 大屏幕显示更多内容（表格、多列布局）
- ✅ 小屏幕自动调整（单列、折叠菜单）
- ❌ 不要让桌面端看起来像手机APP

### 3. 间距系统
```css
--spacing-xs: 0.25rem   /* 4px */
--spacing-sm: 0.5rem    /* 8px */
--spacing-md: 0.75rem   /* 12px */
--spacing-lg: 1rem      /* 16px */
--spacing-xl: 1.5rem    /* 24px */
--spacing-2xl: 2rem     /* 32px */
```

### 4. 圆角系统
```css
--radius-sm: 4px   /* 小元素 */
--radius-md: 6px   /* 按钮、输入框 */
--radius-lg: 8px   /* 卡片 */
```

### 5. 阴影系统
```css
--shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.05)
--shadow-md: 0 2px 8px rgba(0, 0, 0, 0.06)
--shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.08)
```

## 📱 响应式断点

```css
/* 移动端 */
@media (max-width: 640px) { }

/* 平板 */
@media (max-width: 1024px) { }

/* 桌面 */
@media (min-width: 1025px) { }
```

## 🎯 已重构页面

| 页面 | 状态 | 特点 |
|------|------|------|
| **LoginView** | ✅ | 双栏布局，左侧品牌展示 |
| **RegisterView** | ✅ | 双栏布局，功能列表 |
| **HomeView** | ✅ | 响应式栅格，统计卡片 |
| **QuestionBanksView** | ✅ | 表格布局，移动端自适应 |
| **QuizView** | ✅ | 三栏布局，移动端折叠 |
| **AppLayout** | ✅ | 顶部导航，响应式菜单 |
| **AuthLayout** | ✅ | 纯容器，无宽度限制 |

## 🚀 启动命令

```bash
cd frontend/AnswerMe.Frontend
npm run dev
```

访问: http://localhost:5173

## 📋 开发检查清单

创建新页面时，确保：

- [ ] 不使用 `max-width` 限制容器宽度
- [ ] 使用响应式单位（%、fr、vw）
- [ ] 添加 `@media` 断点
- [ ] 使用配色变量，不写死颜色值
- [ ] 不使用渐变紫色 (#667eea, #764ba2)
- [ ] 在移动端和桌面端分别测试
- [ ] 过渡动画不超过 0.2s
- [ ] 按钮圆角 6px，卡片圆角 8px

## 🎨 组件样式示例

### 按钮
```vue
<el-button type="primary" size="large">
  主要按钮
</el-button>
```

### 卡片
```vue
<el-card shadow="hover">
  内容
</el-card>
```

### 表格
```vue
<el-table :data="data" style="width: 100%">
  <!-- 自动适应容器宽度 -->
</el-table>
```

### 栅格布局
```vue
<el-row :gutter="20">
  <el-col :xs="24" :sm="12" :md="8" :lg="6">
    <!-- 移动端全宽，桌面端 1/4 -->
  </el-col>
</el-row>
```
