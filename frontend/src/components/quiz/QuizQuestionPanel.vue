<script setup lang="ts">
interface Question {
  id: number
  content: string
  type: 'single' | 'multiple' | 'essay'
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
  background: #ffffff;
  border-bottom: 1px solid #e5e7eb;
  padding: 1rem 1.5rem;
}

.dark .question-panel {
  background: #111827;
  border-bottom-color: #374151;
}

.question-meta {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  flex-wrap: wrap;
}

.question-index {
  font-size: 0.875rem;
  color: #6b7280;
}

.dark .question-index {
  color: #9ca3af;
}

.question-tag {
  font-size: 0.75rem;
}

.question-content {
  max-width: none;
}

.question-text {
  font-size: 1rem;
  line-height: 1.625;
  color: #111827;
  margin: 0;
  font-weight: 500;
}

.dark .question-text {
  color: #f9fafb;
}
</style>
