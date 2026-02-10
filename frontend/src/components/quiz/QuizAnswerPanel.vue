<script setup lang="ts">
import { computed } from 'vue'

interface Question {
  id: number
  type: 'single' | 'multiple' | 'boolean' | 'fill' | 'essay'
  options: string[]
}

interface Props {
  question: Question
  answer: string | string[] | undefined
  disabled?: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:answer': [answer: string | string[]]
}>()

// 计算已选答案数量
const selectedCount = computed(() => {
  if (Array.isArray(props.answer)) {
    return props.answer.length
  }
  return props.answer ? 1 : 0
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

const getOptionLabel = (index: number) => {
  return String.fromCharCode(65 + index) // A, B, C, D...
}
</script>

<template>
  <div class="answer-panel">
    <!-- 单选题 -->
    <el-radio-group
      v-if="question.type === 'single'"
      :model-value="answer"
      @update:model-value="handleSingleSelect"
    >
      <div class="options-list">
        <div
          v-for="(option, index) in question.options"
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
    <div v-if="question.type === 'multiple'" class="multiple-choice-container">
      <div class="selection-info">
        <span class="selection-hint">请选择所有正确答案</span>
        <span class="selection-count">已选: {{ selectedCount }} 项</span>
      </div>
      <div class="options-list">
        <div
          v-for="(option, index) in question.options"
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

    <!-- 简答题 -->
    <el-input
      v-if="question.type === 'essay'"
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
</style>
