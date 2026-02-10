# 前端代码审查问题清单

> 审查日期: 2026-02-10
> 审查范围: 前端核心逻辑 + 视图组件

---

## 高优先级问题

### 1. QuizStore - userId 硬编码为 0
**文件**: `frontend/src/stores/quiz.ts:185, 204`

**问题描述**:
```typescript
result.value = {
  ...response,
  userId: 0, // 从 API 响应中可能不包含，需要从上下文获取
  details: []
}
```
`userId` 硬编码为 0，注释说明需要从上下文获取但未实现。

**修复建议**:
```typescript
import { useAuthStore } from './auth'

// 在 completeQuiz 和 fetchQuizResult 中
const authStore = useAuthStore()
result.value = {
  ...response,
  userId: authStore.user?.id || 0,
  details: []
}
```

---

### 2. QuizView - TODO 注释未实现功能
**文件**: `frontend/src/views/QuizView.vue:335`

**问题描述**:
```typescript
const toggleDarkMode = () => {
  darkMode.value = !darkMode.value
  // TODO: 实现深色模式切换逻辑
}
```
深色模式切换按钮已存在但实际功能未实现，用户点击无效果。

**修复建议**:
- 方案1: 移除深色模式按钮
- 方案2: 使用 VueUse 的 `useDark()` 实现
```typescript
import { useDark, useToggle } from '@vueuse/core'

const isDark = useDark()
const toggleDark = useToggle(isDark)
```

---

## 中优先级问题

### 3. quiz.ts - QuizDetail 类型定义与后端不一致
**文件**: `frontend/src/api/quiz.ts:47-59`

**问题描述**:
```typescript
export interface QuizDetail {
  questionType: string  // 后端返回的格式可能不同
  options?: string
  userAnswer?: string | string[]  // 后端是 string?
}
```
前端类型定义与后端不完全匹配。

**修复建议**: 统一前后端类型定义，参考后端 DTO 结构。

---

### 4. QuizStore - 答案格式转换可能丢失信息
**文件**: `frontend/src/stores/quiz.ts:145-148`

**问题描述**:
```typescript
const answerString = Array.isArray(userAnswer)
  ? userAnswer.join(',')
  : userAnswer
```
如果选项内容本身包含逗号，`join(',')` 会导致解析歧义。

**修复建议**: 使用 JSON 格式传输
```typescript
const answerString = Array.isArray(userAnswer)
  ? JSON.stringify(userAnswer)
  : userAnswer
```

---

### 5. PracticeView - 重复的题库验证逻辑
**文件**: `frontend/src/views/PracticeView.vue:47-76`

**问题描述**:
先检查 `questionCount`，然后又重新获取列表再次检查，存在冗余逻辑。

**修复建议**: 移除冗余检查或使用乐观更新。

---

### 6. errorHandler.ts 与 request.ts 错误处理不统一
**文件**: `frontend/src/utils/errorHandler.ts`, `frontend/src/utils/request.ts`

**问题描述**:
- `request.ts` 使用 `ElMessage.error` 显示错误
- `errorHandler.ts` 也存在但未充分利用
- 各组件中也直接使用 `ElMessage.error`

**修复建议**: 统一使用 `errorHandler.ts` 处理所有错误。

---

### 7. QuizView - 空值处理冗余
**文件**: `frontend/src/views/QuizView.vue:175-176`

**问题描述**:
```typescript
const q = response?.data ?? response
```
API 已经解包返回数据，不需要双重处理。

**修复建议**: 直接使用 `response`，移除 `?.data` 处理。

---

## 低优先级问题

### 8. 魔术数字和字符串
**位置**: 多处

**问题描述**:
- `PracticeView.vue:29` - `pageSize: 100`
- `request.ts:16` - `timeout: 120000`

**修复建议**: 提取为配置常量。

---

### 9. localStorage 使用缺少错误处理
**文件**: `frontend/src/views/QuizView.vue:271-291`

**问题描述**:
```typescript
localStorage.setItem(key, JSON.stringify([...markedQuestions.value]))
// ...
const saved = localStorage.getItem(key)
```
直接使用 localStorage 可能因配额超限或禁用而报错。

**修复建议**: 添加 try-catch 错误处理。

---

### 10. 类型定义分散
**位置**: `frontend/src/types/`, `frontend/src/api/*.ts`

**问题描述**:
类型定义分散在多个文件中，部分类型在 API 文件中定义，部分在 types 目录中。

**修复建议**: 统一类型定义位置，或考虑使用 API 代码生成工具。

---

## 设计建议

### 1. 考虑使用 Pinia 插件实现持久化
对于需要持久化的状态（如 authStore），考虑使用 `pinia-plugin-persistedstate`。

### 2. 添加请求取消机制
使用 `axios CancelToken` 或 `AbortController` 处理组件卸载时的未完成请求。

### 3. 统一 API 响应格式
确保所有 API 响应遵循统一格式，便于类型定义和错误处理。

### 4. 添加前端性能监控
考虑添加性能监控（如 Web Vitals）和错误追踪（如 Sentry）。

---

## 统计

| 优先级 | 数量 |
|--------|------|
| 高 | 2 |
| 中 | 5 |
| 低 | 3 |
| **合计** | **10** |
