<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElConfigProvider } from 'element-plus'
// @ts-ignore - Element Plus locale types not available
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
import AppLayout from './layouts/AppLayout.vue'
import AuthLayout from './layouts/AuthLayout.vue'

const route = useRoute()

const layout = computed(() => {
  const layoutName = route.meta?.layout || 'app'
  return layoutName === 'auth' ? AuthLayout : AppLayout
})
</script>

<template>
  <ElConfigProvider :locale="zhCn">
    <component :is="layout" />
  </ElConfigProvider>
</template>

<style>
/* 全局样式重置 */
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html,
body {
  height: 100%;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial,
    'Noto Sans', sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji', 'Segoe UI Symbol',
    'Noto Color Emoji';
  font-size: 14px;
  line-height: 1.5;
  color: #303133;
  background-color: #f0f2f5;
}

#app {
  height: 100%;
}

/* 滚动条样式 */
::-webkit-scrollbar {
  width: 8px;
  height: 8px;
}

::-webkit-scrollbar-thumb {
  background: #dcdfe6;
  border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
  background: #c0c4cc;
}

::-webkit-scrollbar-track {
  background: transparent;
}

/* Element Plus 样式优化 */
.el-card {
  border-radius: 12px;
  border: 1px solid #ebeef5;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.05);
}

.el-button {
  border-radius: 8px;
  font-weight: 500;
}

.el-input__wrapper {
  border-radius: 8px;
}

.el-select .el-input__wrapper {
  border-radius: 8px;
}

/* 动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
