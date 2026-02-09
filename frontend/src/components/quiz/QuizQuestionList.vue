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
  width: 14rem;
  background: #ffffff;
  border-right: 1px solid #e5e7eb;
  display: flex;
  flex-direction: column;
}

.dark .question-list {
  background: #111827;
  border-right-color: #374151;
}

.list-header {
  padding: 0.75rem;
  border-bottom: 1px solid #e5e7eb;
}

.dark .list-header {
  border-bottom-color: #374151;
}

.list-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
  margin: 0;
}

.dark .list-title {
  color: #f9fafb;
}

.list-content {
  flex: 1;
  overflow-y: auto;
  padding: 0.5rem;
}

.question-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.375rem;
}

.question-item {
  aspect-ratio: 1;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  background: #ffffff;
  cursor: pointer;
  transition: all 0.15s;
}

.dark .question-item {
  background: #1f2937;
  border-color: #374151;
}

.question-item:hover {
  border-color: #d1d5db;
}

.dark .question-item:hover {
  border-color: #4b5563;
}

.question-item.current {
  ring: 2px solid #3b82f6;
  ring-offset: 2px;
}

.question-item.answered {
  background: #eff6ff;
  border-color: #bfdbfe;
}

.dark .question-item.answered {
  background: #1e3a8a33;
  border-color: #1e40af;
}

.question-item.marked {
  background: #fffbeb;
  border-color: #fcd34d;
}

.dark .question-item.marked {
  background: #451a0333;
  border-color: #b45309;
}

.question-number {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.dark .question-number {
  color: #d1d5db;
}

.difficulty-dot {
  position: absolute;
  top: 0.125rem;
  left: 0.125rem;
  width: 0.375rem;
  height: 0.375rem;
  border-radius: 9999px;
}

.status-icon {
  position: absolute;
  font-size: 0.75rem;
}

.status-icon.answered {
  top: -0.25rem;
  right: -0.25rem;
  color: #3b82f6;
}

.status-icon.marked {
  bottom: -0.125rem;
  right: -0.125rem;
  color: #f59e0b;
}

/* 图例 */
.list-legend {
  padding: 0.75rem;
  border-top: 1px solid #e5e7eb;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.dark .list-legend {
  border-top-color: #374151;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.legend-box {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 0.25rem;
}

.legend-box.answered {
  background: #eff6ff;
  border: 1px solid #bfdbfe;
}

.dark .legend-box.answered {
  background: #1e3a8a33;
  border-color: #1e40af;
}

.legend-box.marked {
  background: #fffbeb;
  border: 1px solid #fcd34d;
}

.dark .legend-box.marked {
  background: #451a0333;
  border-color: #b45309;
}

.legend-dots {
  display: flex;
  gap: 0.125rem;
}

.legend-dot {
  width: 0.25rem;
  height: 0.25rem;
  border-radius: 9999px;
}

.legend-text {
  font-size: 0.75rem;
  color: #6b7280;
}

.dark .legend-text {
  color: #9ca3af;
}

/* 滚动条 */
.list-content::-webkit-scrollbar {
  width: 4px;
}

.list-content::-webkit-scrollbar-thumb {
  background: rgba(156, 163, 175, 0.5);
  border-radius: 2px;
}

.list-content::-webkit-scrollbar-thumb:hover {
  background: rgba(156, 163, 175, 0.7);
}
</style>
