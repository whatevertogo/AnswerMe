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

<!-- 全局样式已统一移至 src/styles/theme.css -->
