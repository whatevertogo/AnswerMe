<script setup lang="ts">
import { DifficultyLabels, DifficultyColors } from '@/types/question'
import type { QuizQuestion } from '@/stores/quiz'

interface Props {
  question: QuizQuestion
  questionNumber: number
  totalQuestions: number
}

defineProps<Props>()

const getDifficultyClass = (difficulty: string): 'success' | 'warning' | 'danger' | 'info' => {
  return DifficultyColors[difficulty as keyof typeof DifficultyColors] || 'info'
}

const getDifficultyLabel = (difficulty: string): string => {
  return DifficultyLabels[difficulty as keyof typeof DifficultyLabels] || '未知'
}

const getTypeLabel = (type: string): string => {
  const typeLabels: Record<string, string> = {
    single: '单选题',
    multiple: '多选题',
    boolean: '判断题',
    fill: '填空题',
    essay: '简答题'
  }
  return typeLabels[type] || '未知'
}
</script>

<template>
  <div class="question-panel">
    <!-- 题目元信息 -->
    <div class="question-meta">
      <span class="question-index"> 第 {{ questionNumber }} 题 / 共 {{ totalQuestions }} 题 </span>
      <el-divider direction="vertical" />
      <el-tag :type="getDifficultyClass(question.difficulty)" size="small">
        {{ getDifficultyLabel(question.difficulty) }}
      </el-tag>
      <el-tag type="info" size="small" effect="plain">
        {{ getTypeLabel(question.type) }}
      </el-tag>
      <el-tag
        v-for="tag in question.tags"
        :key="tag"
        size="small"
        effect="plain"
        class="question-tag"
      >
        {{ tag }}
      </el-tag>
    </div>

    <!-- 题目内容 -->
    <div class="question-content">
      <p class="question-text">{{ question.content }}</p>
    </div>
  </div>
</template>

<style scoped>
.question-panel {
  @apply bg-bg border-b border-border px-8 py-6;
}

.dark .question-panel {
  @apply bg-bg border-b-border;
}

.question-meta {
  @apply flex items-center gap-2 mb-4 flex-wrap;
}

.question-index {
  @apply text-sm text-text-secondary;
}

.dark .question-index {
  @apply text-text-secondary;
}

.question-tag {
  @apply text-xs;
}

.question-content {
  @apply max-w-none;
}

.question-text {
  @apply text-[1.125rem] leading-[1.625] text-text-primary m-0 font-medium;
}

.dark .question-text {
  @apply text-text-primary;
}
</style>
