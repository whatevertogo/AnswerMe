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
  @apply max-w-3xl;
}

.options-list {
  @apply flex flex-col gap-3;
}

.option-item {
  @apply px-5 py-4 border-2 border-border rounded-md
         transition-all duration-400 ease-smooth;
}

.dark .option-item {
  @apply border-border;
}

.option-item:hover {
  @apply border-primary;
}

.dark .option-item:hover {
  @apply border-primary;
}

.option-item.is-selected {
  @apply border-primary bg-bg-secondary;
}

.dark .option-item.is-selected {
  @apply border-primary bg-bg-secondary;
}

.option-item.is-disabled {
  @apply opacity-50 cursor-not-allowed;
}

.option-label {
  @apply font-medium text-text-secondary mr-2;
}

.dark .option-label {
  @apply text-text-secondary;
}

.option-text {
  @apply text-text-primary;
}

.dark .option-text {
  @apply text-text-primary;
}

.essay-input {
  @apply w-full;
}

.essay-input :deep(.el-textarea__inner) {
  @apply text-sm leading-[1.625];
}

.multiple-choice-container {
  @apply flex flex-col gap-4;
}

.selection-info {
  @apply flex justify-between items-center px-4 py-3 bg-bg-secondary rounded-md text-sm;
}

.dark .selection-info {
  @apply bg-bg-secondary;
}

.selection-hint {
  @apply text-text-secondary font-medium;
}

.dark .selection-hint {
  @apply text-text-secondary;
}

.selection-count {
  @apply text-primary font-semibold;
}

/* 判断题 */
.boolean-container {
  @apply flex justify-center py-4;
}

.boolean-options {
  @apply flex gap-8;
}

.boolean-option {
  @apply px-10 py-4 border-2 border-border rounded-lg
         transition-all duration-400 ease-smooth;
}

.dark .boolean-option {
  @apply border-border;
}

.boolean-option:hover {
  @apply border-primary;
}

.boolean-option.is-checked {
  @apply border-primary bg-bg-secondary;
}

.dark .boolean-option.is-checked {
  @apply border-primary bg-bg-secondary;
}

.boolean-option-text {
  @apply text-base font-medium;
}

/* 填空题 */
.fill-container {
  @apply flex flex-col gap-4;
}

.fill-hint {
  @apply flex items-center gap-2 px-4 py-3.5
         bg-bg-secondary border border-border rounded-md text-sm text-text-primary;
}

.dark .fill-hint {
  @apply bg-bg-secondary border-border text-text-primary;
}

.fill-input {
  @apply w-full;
}

.fill-input :deep(.el-textarea__inner) {
  @apply text-sm leading-[1.625];
}
</style>
