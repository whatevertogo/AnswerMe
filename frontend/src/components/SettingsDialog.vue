<script setup lang="ts">
import { useTheme } from '@/composables/useTheme'

interface Props {
  modelValue: boolean
}

interface Emits {
  (e: 'update:modelValue', value: boolean): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const { themeMode, setTheme } = useTheme()

const handleClose = () => {
  emit('update:modelValue', false)
}

const handleThemeChange = (mode: 'system' | 'light' | 'dark') => {
  setTheme(mode)
}
</script>

<template>
  <el-dialog
    :model-value="modelValue"
    title="设置"
    width="400px"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="settings-container">
      <!-- 主题设置 -->
      <div class="setting-section">
        <h3 class="section-title">主题</h3>
        <el-radio-group :model-value="themeMode" @change="handleThemeChange">
          <div class="theme-option">
            <el-radio value="system">跟随系统</el-radio>
          </div>
          <div class="theme-option">
            <el-radio value="light">浅色模式</el-radio>
          </div>
          <div class="theme-option">
            <el-radio value="dark">深色模式</el-radio>
          </div>
        </el-radio-group>
      </div>
    </div>

    <template #footer>
      <el-button type="primary" @click="handleClose">确定</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.settings-container {
  padding: 8px 0;
}

.setting-section {
  margin-bottom: 24px;
}

.setting-section:last-child {
  margin-bottom: 0;
}

.section-title {
  font-size: 14px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin: 0 0 12px 0;
}

.theme-option {
  padding: 8px 0;
}

:deep(.el-radio) {
  margin-right: 12px;
}

:deep(.el-radio__label) {
  color: var(--color-text-primary);
}
</style>
