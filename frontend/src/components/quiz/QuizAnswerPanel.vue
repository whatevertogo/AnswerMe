<script setup lang="ts">
import { computed } from 'vue'
import { InfoFilled } from '@element-plus/icons-vue'
import type { QuizQuestion } from '@/stores/quiz'

interface Props {
  question: QuizQuestion
  answer: string | string[] | undefined
  disabled?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:answer': [answer: string | string[]]
}>()

// 选项列表
const options = computed(() => props.question.options || [])

// 题目类型（由 QuizQuestion 直接提供）
const currentType = computed(() => props.question.type || 'essay')

// 计算已选答案数量
const selectedCount = computed(() => {
  if (Array.isArray(props.answer)) {
    return props.answer.length
  }
  return props.answer ? 1 : 0
})

// 判断题：获取当前选中值（转换为布尔值）
const booleanValue = computed(() => {
  if (typeof props.answer === 'string') {
    return props.answer === 'true'
  }
  if (typeof props.answer === 'boolean') {
    return props.answer
  }
  return undefined
})

const handleSingleSelect = (option: string) => {
  emit('update:answer', option)
}

const handleMultipleSelect = (option: string, checked: boolean | string[]) => {
  const currentAnswers = (props.answer as string[]) || []
  // el-checkbox 可能传递 boolean 或 string[]
  const isChecked = typeof checked === 'boolean' ? checked : checked.includes(option)
  if (isChecked) {
    emit('update:answer', [...currentAnswers, option])
  } else {
    emit('update:answer', currentAnswers.filter(a => a !== option))
  }
}

const handleEssayChange = (value: string) => {
  emit('update:answer', value)
}

const handleBooleanChange = (value: boolean) => {
  emit('update:answer', value.toString())
}

const getOptionLabel = (index: number) => {
  return String.fromCharCode(65 + index) // A, B, C, D...
}
</script>

<template>
  <div class="answer-panel">
    <!-- 单选题 -->
    <el-radio-group
      v-if="currentType === 'single'"
      :model-value="answer"
      @update:model-value="handleSingleSelect"
    >
      <div class="options-list">
        <div
          v-for="(option, index) in options"
          :key="index"
          :class="['option-item', { 'is-selected': answer === option }]"
        >
          <el-radio :value="option" :disabled="disabled">
            <span class="option-label">{{ getOptionLabel(index) }}.</span>
            <span class="option-text">{{ option }}</span>
          </el-radio>
        </div>
      </div>
    </el-radio-group>

    <!-- 多选题 -->
    <div v-if="currentType === 'multiple'" class="multiple-choice-container">
      <div class="selection-info">
        <span class="selection-hint">请选择所有正确答案</span>
        <span class="selection-count">已选: {{ selectedCount }} 项</span>
      </div>
      <div class="options-list">
        <div
          v-for="(option, index) in options"
          :key="index"
          :class="['option-item', { 'is-selected': (answer as string[])?.includes(option) }]"
        >
          <el-checkbox
            :model-value="(answer as string[])?.includes(option)"
            @update:model-value="(val: any) => handleMultipleSelect(option, val as boolean)"
            :disabled="disabled"
          >
            <span class="option-label">{{ getOptionLabel(index) }}.</span>
            <span class="option-text">{{ option }}</span>
          </el-checkbox>
        </div>
      </div>
    </div>

    <!-- 判断题 -->
    <div v-if="currentType === 'boolean'" class="boolean-container">
      <el-radio-group
        :model-value="booleanValue"
        @update:model-value="handleBooleanChange"
        :disabled="disabled"
        class="boolean-options"
      >
        <el-radio :value="true" size="large" class="boolean-option">
          <span class="boolean-option-text">正确</span>
        </el-radio>
        <el-radio :value="false" size="large" class="boolean-option">
          <span class="boolean-option-text">错误</span>
        </el-radio>
      </el-radio-group>
    </div>

    <!-- 填空题 -->
    <div v-if="currentType === 'fill'" class="fill-container">
      <div class="fill-hint">
        <el-icon><InfoFilled /></el-icon>
        <span>请填写所有空白，多个答案用逗号分隔</span>
      </div>
      <el-input
        :model-value="Array.isArray(answer) ? answer.join(', ') : answer"
        @update:model-value="handleEssayChange"
        type="textarea"
        :rows="4"
        placeholder="请输入答案，多个答案用逗号分隔..."
        :disabled="disabled"
        resize="vertical"
        class="fill-input"
      />
    </div>

    <!-- 简答题 -->
    <el-input
      v-if="currentType === 'essay'"
      :model-value="answer"
      @update:model-value="handleEssayChange"
      type="textarea"
      :rows="8"
      placeholder="请输入你的答案..."
      :disabled="disabled"
      resize="vertical"
      class="essay-input"
    />
  </div>
</template>

<style scoped>
.answer-panel {
  max-width: 48rem;
}

.options-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.option-item {
  padding: 0.75rem 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  transition: all 0.15s;
}

.dark .option-item {
  border-color: #374151;
}

.option-item:hover {
  border-color: #d1d5db;
}

.dark .option-item:hover {
  border-color: #4b5563;
}

.option-item.is-selected {
  border-color: #3b82f6;
  background: #eff6ff;
}

.dark .option-item.is-selected {
  border-color: #3b82f6;
  background: #1e3a8a33;
}

.option-item.is-disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.option-label {
  font-weight: 500;
  color: #6b7280;
  margin-right: 0.5rem;
}

.dark .option-label {
  color: #9ca3af;
}

.option-text {
  color: #374151;
}

.dark .option-text {
  color: #d1d5db;
}

.essay-input {
  width: 100%;
}

.essay-input :deep(.el-textarea__inner) {
  font-size: 0.875rem;
  line-height: 1.625;
}

.multiple-choice-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.selection-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.5rem 0.75rem;
  background: #f9fafb;
  border-radius: 0.375rem;
  font-size: 0.875rem;
}

.dark .selection-info {
  background: #1f2937;
}

.selection-hint {
  color: #6b7280;
  font-weight: 500;
}

.dark .selection-hint {
  color: #9ca3af;
}

.selection-count {
  color: #3b82f6;
  font-weight: 600;
}

/* 判断题 */
.boolean-container {
  display: flex;
  justify-content: center;
  padding: 1rem 0;
}

.boolean-options {
  display: flex;
  gap: 2rem;
}

.boolean-option {
  padding: 1rem 2rem;
  border: 2px solid #e5e7eb;
  border-radius: 0.5rem;
  transition: all 0.2s;
}

.dark .boolean-option {
  border-color: #374151;
}

.boolean-option:hover {
  border-color: #3b82f6;
}

.boolean-option.is-checked {
  border-color: #3b82f6;
  background: #eff6ff;
}

.dark .boolean-option.is-checked {
  border-color: #3b82f6;
  background: #1e3a8a33;
}

.boolean-option-text {
  font-size: 1rem;
  font-weight: 500;
}

/* 填空题 */
.fill-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.fill-hint {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  background: #f0f9ff;
  border: 1px solid #bae6fd;
  border-radius: 0.5rem;
  font-size: 0.875rem;
  color: #0369a1;
}

.dark .fill-hint {
  background: #0c4a6e33;
  border-color: #075985;
  color: #7dd3fc;
}

.fill-input {
  width: 100%;
}

.fill-input :deep(.el-textarea__inner) {
  font-size: 0.875rem;
  line-height: 1.625;
}
</style>
