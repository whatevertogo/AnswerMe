<script setup lang="ts">
interface Question {
  id: number
  content: string
  type: 'single' | 'multiple' | 'boolean' | 'fill' | 'essay'
  difficulty: string
  tags: string[]
}

interface Props {
  question: Question
  questionNumber: number
  totalQuestions: number
}

const props = defineProps<Props>()

const getDifficultyClass = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return 'success'
    case 'medium': return 'warning'
    case 'hard': return 'danger'
    default: return 'info'
  }
}

const getDifficultyLabel = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return '简单'
    case 'medium': return '中等'
    case 'hard': return '困难'
    default: return '未知'
  }
}

const getTypeLabel = (type: string) => {
  switch (type) {
    case 'single': return '单选题'
    case 'multiple': return '多选题'
    case 'essay': return '简答题'
    default: return '未知'
  }
}
</script>

<template>
  <div class="question-panel">
    <!-- 题目元信息 -->
    <div class="question-meta">
      <span class="question-index">
        第 {{ questionNumber }} 题 / 共 {{ totalQuestions }} 题
      </span>
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
