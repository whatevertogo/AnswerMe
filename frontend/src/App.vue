<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { ElConfigProvider } from 'element-plus'
import AppLayout from './layouts/AppLayout.vue'
import AuthLayout from './layouts/AuthLayout.vue'

// @ts-ignore - Element Plus locale module doesn't have type definitions
const zhCn = ref<any>(null)

const route = useRoute()

const layout = computed(() => {
  const layoutName = route.meta?.layout || 'app'
  return layoutName === 'auth' ? AuthLayout : AppLayout
})

onMounted(async () => {
  // 动态导入Element Plus中文 locale，避免类型问题
  // @ts-ignore
  const module = await import('element-plus/dist/locale/zh-cn.mjs')
  zhCn.value = module.default
})
</script>

<template>
  <ElConfigProvider :locale="zhCn">
    <component :is="layout" />
  </ElConfigProvider>
</template>

<!-- 全局样式已统一移至 src/styles/theme.css -->
