<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElConfigProvider } from 'element-plus'
import zhCn from 'element-plus/es/locale/lang/zh-cn'
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
