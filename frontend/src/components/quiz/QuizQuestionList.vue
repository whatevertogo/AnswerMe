<script setup lang="ts">
import { CircleCheck, Flag } from '@element-plus/icons-vue'

interface Question {
  id: number
  difficulty: string
}

interface Props {
  questions: Question[]
  currentIndex: number
  answers: Record<number, string | string[]>
  markedQuestions: Set<number>
}

const props = defineProps<Props>()

const emit = defineEmits<{
  questionClick: [index: number]
}>()

const getDifficultyColor = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return '#10b981'
    case 'medium': return '#f59e0b'
    case 'hard': return '#ef4444'
    default: return '#6b7280'
  }
}

const getStatusClass = (index: number) => {
  const question = props.questions[index]
  if (!question) return ''

  const hasAnswer = props.answers[question.id] !== undefined
  const isMarked = props.markedQuestions.has(question.id)

  if (index === props.currentIndex) {
    return 'current'
  }
  if (isMarked) {
    return 'marked'
  }
  if (hasAnswer) {
    return 'answered'
  }
  return ''
}
</script>

<template>
  <div class="question-list">
    <div class="list-header">
      <h3 class="list-title">题目列表</h3>
    </div>

    <div class="list-content">
      <div class="question-grid">
        <button
          v-for="(question, index) in questions"
          :key="question.id"
          :class="['question-item', getStatusClass(index)]"
          @click="emit('questionClick', index)"
        >
          <span class="question-number">{{ index + 1 }}</span>

          <!-- 难度指示器 -->
          <div
            class="difficulty-dot"
            :style="{ backgroundColor: getDifficultyColor(question.difficulty) }"
          />

          <!-- 已答标记 -->
          <el-icon v-if="answers[question.id] !== undefined" class="status-icon answered">
            <CircleCheck />
          </el-icon>

          <!-- 标记标记 -->
          <el-icon v-if="markedQuestions.has(question.id)" class="status-icon marked">
            <Flag />
          </el-icon>
        </button>
      </div>
    </div>

    <!-- 图例 -->
    <div class="list-legend">
      <div class="legend-item">
        <div class="legend-box answered" />
        <span class="legend-text">已答题</span>
      </div>
      <div class="legend-item">
        <div class="legend-box marked" />
        <span class="legend-text">已标记</span>
      </div>
      <div class="legend-item">
        <div class="legend-dots">
          <div class="legend-dot" style="background: #10b981" />
          <div class="legend-dot" style="background: #f59e0b" />
          <div class="legend-dot" style="background: #ef4444" />
        </div>
        <span class="legend-text">难度</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.question-list {
  @apply w-56 bg-bg border-r border-border flex flex-col;
}

.dark .question-list {
  @apply bg-bg border-r-border;
}

.list-header {
  @apply px-3 py-3 border-b border-border;
}

.dark .list-header {
  @apply border-b-border;
}

.list-title {
  @apply text-sm font-semibold text-text-primary m-0;
}

.dark .list-title {
  @apply text-text-primary;
}

.list-content {
  @apply flex-1 overflow-y-auto p-2;
}

.question-grid {
  @apply grid grid-cols-4 gap-2;
}

.question-item {
  @apply aspect-square relative flex items-center justify-center
         border-2 border-border rounded-md bg-bg
         cursor-pointer transition-all duration-400 ease-smooth;
}

.dark .question-item {
  @apply bg-bg border-border;
}

.question-item:hover {
  @apply border-primary scale-105;
}

.dark .question-item:hover {
  @apply border-primary;
}

.question-item.current {
  @apply border-primary;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.question-item.answered {
  @apply bg-bg-secondary border-primary;
}

.dark .question-item.answered {
  @apply bg-bg-secondary border-primary;
}

.question-item.marked {
  @apply bg-bg-secondary border-warning;
}

.dark .question-item.marked {
  @apply bg-bg-secondary border-warning;
}

.question-number {
  @apply text-sm font-medium text-text-primary;
}

.dark .question-number {
  @apply text-text-primary;
}

.difficulty-dot {
  @apply absolute top-1 left-1 w-1.5 h-1.5 rounded-full;
}

.status-icon {
  @apply absolute text-xs;
}

.status-icon.answered {
  @apply -top-1 -right-1 text-primary;
}

.status-icon.marked {
  @apply -bottom-0.5 -right-0.5 text-warning;
}

/* 图例 */
.list-legend {
  @apply px-3 py-3 border-t border-border flex flex-col gap-2;
}

.dark .list-legend {
  @apply border-t-border;
}

.legend-item {
  @apply flex items-center gap-2;
}

.legend-box {
  @apply w-3 h-3 rounded-sm;
}

.legend-box.answered {
  @apply bg-bg-secondary border border-primary;
}

.dark .legend-box.answered {
  @apply bg-bg-secondary border-primary;
}

.legend-box.marked {
  @apply bg-bg-secondary border border-warning;
}

.dark .legend-box.marked {
  @apply bg-bg-secondary border-warning;
}

.legend-dots {
  @apply flex gap-0.5;
}

.legend-dot {
  @apply w-1 h-1 rounded-full;
}

.legend-text {
  @apply text-xs text-text-secondary;
}

.dark .legend-text {
  @apply text-text-secondary;
}

/* 滚动条 */
.list-content::-webkit-scrollbar {
  width: 4px;
}

.list-content::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 2px;
}

.list-content::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-muted);
}
</style>
