<script setup lang="ts">
import { ref, computed } from 'vue'
import { CircleCheck, Clock, Aim, RefreshRight, ArrowUp } from '@element-plus/icons-vue'

interface Question {
  id: number
  content: string
  type: 'single' | 'multiple' | 'boolean' | 'fill' | 'essay'
  difficulty: string
  tags: string[]
  correctAnswer?: string
  explanation?: string
}

interface Props {
  visible: boolean
  questions: Question[]
  answers: Record<number, string | string[]>
  timeElapsed: number
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
}>()

const activeTab = ref('overview')

const answeredCount = computed(() => Object.keys(props.answers).length)

// 计算实际答对的题目数量
const correctCount = computed(() => {
  let count = 0
  props.questions.forEach(question => {
    const userAnswer = props.answers[question.id]
    if (!userAnswer) return

    // 将用户答案和正确答案标准化后比较
    const normalizeAnswer = (answer: string | string[]): string => {
      if (Array.isArray(answer)) {
        return answer.sort().join(',')
      }
      return answer.trim().toLowerCase()
    }

    const userAnswerStr = normalizeAnswer(userAnswer)
    const correctAnswerStr = normalizeAnswer(question.correctAnswer || '')

    if (userAnswerStr === correctAnswerStr) {
      count++
    }
  })
  return count
})

const score = computed(() => Math.round((correctCount.value / props.questions.length) * 100))
const accuracy = computed(() => answeredCount.value > 0 ? Math.round((correctCount.value / answeredCount.value) * 100) : 0)

const formatTime = (seconds: number) => {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins}分${secs}秒`
}

const getScoreClass = (score: number) => {
  if (score >= 90) return 'excellent'
  if (score >= 70) return 'good'
  if (score >= 60) return 'pass'
  return 'fail'
}

const getScoreLabel = (score: number) => {
  if (score >= 90) return '优秀'
  if (score >= 70) return '良好'
  if (score >= 60) return '及格'
  return '需努力'
}

const getDifficultyColor = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return 'var(--color-success)'
    case 'medium': return 'var(--color-warning)'
    case 'hard': return 'var(--color-danger)'
    default: return 'var(--color-text-muted)'
  }
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    @update:model-value="emit('update:visible', $event)"
    title="考试结果"
    width="900px"
    :close-on-click-modal="false"
  >
    <el-tabs v-model="activeTab">
      <!-- 总览 -->
      <el-tab-pane label="总览" name="overview">
        <div class="overview-grid">
          <!-- 得分卡片 -->
          <div class="score-card" :class="getScoreClass(score)">
            <div class="card-header">
              <span class="card-label">总分</span>
              <el-icon :size="20"><Aim /></el-icon>
            </div>
            <div class="score-value">{{ score }}</div>
            <div class="score-label">{{ getScoreLabel(score) }}</div>
          </div>

          <!-- 正确率卡片 -->
          <div class="stat-card success">
            <div class="card-header">
              <span class="card-label">正确率</span>
              <el-icon :size="20" color="#10b981"><ArrowUp /></el-icon>
            </div>
            <div class="stat-value">{{ accuracy }}%</div>
            <div class="stat-label">{{ correctCount }}/{{ answeredCount }} 题</div>
          </div>

          <!-- 用时卡片 -->
          <div class="stat-card purple">
            <div class="card-header">
              <span class="card-label">用时</span>
              <el-icon :size="20" color="#8b5cf6"><Clock /></el-icon>
            </div>
            <div class="stat-value">{{ formatTime(timeElapsed) }}</div>
            <div class="stat-label">平均每题 {{ Math.round(timeElapsed / questions.length) }} 秒</div>
          </div>
        </div>

        <!-- 进度条 -->
        <div class="progress-section">
          <div class="progress-item">
            <div class="progress-header">
              <span class="progress-label">答题进度</span>
              <span class="progress-value">{{ answeredCount }}/{{ questions.length }}</span>
            </div>
            <el-progress :percentage="(answeredCount / questions.length) * 100" :stroke-width="8" />
          </div>

          <div class="progress-item">
            <div class="progress-header">
              <span class="progress-label">正确率</span>
              <span class="progress-value">{{ accuracy }}%</span>
            </div>
            <el-progress :percentage="accuracy" :stroke-width="8" color="#10b981" />
          </div>
        </div>

        <!-- 操作按钮 -->
        <div class="action-buttons">
          <el-button :icon="RefreshRight">再做一次</el-button>
          <el-button type="primary">返回首页</el-button>
        </div>
      </el-tab-pane>

      <!-- 答题详情 -->
      <el-tab-pane label="答题详情" name="details">
        <div class="details-list">
          <div
            v-for="(question, index) in questions"
            :key="question.id"
            class="detail-item"
          >
            <div class="detail-header">
              <el-icon
                :class="[
                  'detail-status',
                  answers[question.id] ? 'answered' : 'unanswered'
                ]"
              >
                <CircleCheck />
              </el-icon>
              <span class="detail-index">第 {{ index + 1 }} 题</span>
              <el-tag
                size="small"
                :color="getDifficultyColor(question.difficulty)"
              >
                {{ question.difficulty === 'easy' ? '简单' : question.difficulty === 'medium' ? '中等' : '困难' }}
              </el-tag>
            </div>

            <p class="detail-content">{{ question.content }}</p>

            <div
              v-if="answers[question.id] && question.explanation"
              class="detail-explanation"
            >
              <p class="explanation-label">解析</p>
              <p class="explanation-text">{{ question.explanation }}</p>
            </div>
          </div>
        </div>
      </el-tab-pane>

      <!-- 分析 -->
      <el-tab-pane label="分析" name="analysis">
        <div class="analysis-grid">
          <!-- 按难度统计 -->
          <div class="analysis-card">
            <h4 class="analysis-title">按难度统计</h4>
            <div class="stat-list">
              <div class="stat-item">
                <div class="stat-dot easy" />
                <span class="stat-name">简单</span>
                <span class="stat-count">2/3 (67%)</span>
              </div>
              <div class="stat-item">
                <div class="stat-dot medium" />
                <span class="stat-name">中等</span>
                <span class="stat-count">2/2 (100%)</span>
              </div>
              <div class="stat-item">
                <div class="stat-dot hard" />
                <span class="stat-name">困难</span>
                <span class="stat-count">0/1 (0%)</span>
              </div>
            </div>
          </div>

          <!-- 知识点掌握 -->
          <div class="analysis-card">
            <h4 class="analysis-title">知识点掌握</h4>
            <div class="knowledge-list">
              <div class="knowledge-item">
                <div class="knowledge-header">
                  <span class="knowledge-name">JavaScript</span>
                  <span class="knowledge-percent">80%</span>
                </div>
                <el-progress :percentage="80" :stroke-width="6" :show-text="false" />
              </div>
              <div class="knowledge-item">
                <div class="knowledge-header">
                  <span class="knowledge-name">React</span>
                  <span class="knowledge-percent">70%</span>
                </div>
                <el-progress :percentage="70" :stroke-width="6" :show-text="false" />
              </div>
              <div class="knowledge-item">
                <div class="knowledge-header">
                  <span class="knowledge-name">CSS</span>
                  <span class="knowledge-percent">90%</span>
                </div>
                <el-progress :percentage="90" :stroke-width="6" :show-text="false" />
              </div>
              <div class="knowledge-item">
                <div class="knowledge-header">
                  <span class="knowledge-name">Vue</span>
                  <span class="knowledge-percent">60%</span>
                </div>
                <el-progress :percentage="60" :stroke-width="6" :show-text="false" />
              </div>
            </div>
          </div>
        </div>

        <!-- 改进建议 -->
        <div class="suggestions">
          <h4 class="suggestions-title">改进建议</h4>
          <ul class="suggestions-list">
            <li>• 重点复习 Vue Composition API 相关知识</li>
            <li>• 加强困难题型的练习</li>
            <li>• 提高答题速度,当前平均用时较长</li>
          </ul>
        </div>
      </el-tab-pane>
    </el-tabs>
  </el-dialog>
</template>

<style scoped>
.overview-grid {
  @apply grid grid-cols-3 gap-4 mb-6;
}

.score-card {
  @apply px-6 py-6 rounded-xl;
  background: linear-gradient(to bottom right, #dbeafe, #bfdbfe);
  border: 1px solid #93c5fd;
}

.dark .score-card {
  background: linear-gradient(to bottom right, #1e3a8a33, #1e40af33);
  border-color: #1e40af;
}

.score-card.excellent {
  background: linear-gradient(to bottom right, #d1fae5, #a7f3d0);
  border-color: #6ee7b7;
}

.dark .score-card.excellent {
  background: linear-gradient(to bottom right, #064e3b33, #065f4633);
  border-color: #059669;
}

.stat-card {
  @apply px-6 py-6 rounded-xl bg-[#f9fafb] border border-[#e5e7eb];
}

.dark .stat-card {
  background: #1f2937;
  border-color: #374151;
}

.stat-card.success {
  background: linear-gradient(to bottom right, #d1fae5, #a7f3d0);
  border-color: #6ee7b7;
}

.dark .stat-card.success {
  background: linear-gradient(to bottom right, #064e3b33, #065f4633);
  border-color: #059669;
}

.stat-card.purple {
  background: linear-gradient(to bottom right, #ede9fe, #ddd6fe);
  border-color: #c4b5fd;
}

.dark .stat-card.purple {
  background: linear-gradient(to bottom right, #4c1d9533, #5b21b633);
  border-color: #6d28d9;
}

.card-header {
  @apply flex items-center justify-between mb-2;
}

.card-label {
  @apply text-sm text-[#6b7280];
}

.dark .card-label {
  color: #9ca3af;
}

.score-value {
  @apply text-[2.25rem] font-bold leading-none mb-1;
  color: #111827;
}

.dark .score-value {
  color: #f9fafb;
}

.stat-value {
  @apply text-[2.25rem] font-bold leading-none mb-1;
  color: #111827;
}

.dark .stat-value {
  color: #f9fafb;
}

.score-label,
.stat-label {
  @apply text-sm text-[#6b7280];
}

.dark .score-label,
.dark .stat-label {
  color: #9ca3af;
}

.progress-section {
  @apply flex flex-col gap-4 mb-6;
}

.progress-item {
  @apply flex flex-col gap-1.5;
}

.progress-header {
  @apply flex items-center justify-between;
}

.progress-label {
  @apply text-sm font-medium;
  color: #374151;
}

.dark .progress-label {
  color: #d1d5db;
}

.progress-value {
  @apply text-sm text-[#6b7280];
}

.dark .progress-value {
  color: #9ca3af;
}

.action-buttons {
  @apply flex gap-3;
}

/* 详情列表 */
.details-list {
  @apply flex flex-col gap-3 max-h-[500px] overflow-y-auto;
}

.detail-item {
  @apply px-4 py-4 border border-[#e5e7eb] rounded-md bg-bg;
}

.dark .detail-item {
  background: #1f2937;
  border-color: #374151;
}

.detail-header {
  @apply flex items-center gap-2 mb-2;
}

.detail-status {
  @apply text-base;
}

.detail-status.answered {
  @apply text-[#10b981];
}

.detail-status.unanswered {
  @apply text-[#d1d5db];
}

.detail-index {
  @apply text-sm font-medium text-[#6b7280];
}

.dark .detail-index {
  color: #9ca3af;
}

.detail-content {
  @apply text-sm leading-[1.5] mb-2 m-0;
  color: #374151;
}

.dark .detail-content {
  color: #d1d5db;
}

.detail-explanation {
  @apply mt-3 px-3 py-3 bg-[#dbeafe] rounded-md border border-[#93c5fd];
}

.dark .detail-explanation {
  background: #1e3a8a33;
  border-color: #1e40af;
}

.explanation-label {
  @apply text-xs font-semibold mb-1 m-0;
  color: #1e40af;
}

.dark .explanation-label {
  color: #60a5fa;
}

.explanation-text {
  @apply text-xs m-0;
  color: #1e3a8a;
}

.dark .explanation-text {
  color: #93c5fd;
}

/* 分析 */
.analysis-grid {
  @apply grid grid-cols-2 gap-4 mb-4;
}

.analysis-card {
  @apply px-4 py-4 bg-[#f9fafb] border border-[#e5e7eb] rounded-md;
}

.dark .analysis-card {
  background: #1f2937;
  border-color: #374151;
}

.analysis-title {
  @apply text-sm font-semibold mb-3 m-0;
  color: #111827;
}

.dark .analysis-title {
  color: #f9fafb;
}

.stat-list {
  @apply flex flex-col gap-2;
}

.stat-item {
  @apply flex items-center gap-2;
}

.stat-dot {
  @apply w-2 h-2 rounded-full;
}

.stat-dot.easy {
  @apply bg-[#10b981];
}

.stat-dot.medium {
  @apply bg-[#f59e0b];
}

.stat-dot.hard {
  @apply bg-[#ef4444];
}

.stat-name {
  @apply text-sm flex-1;
  color: #374151;
}

.dark .stat-name {
  color: #d1d5db;
}

.stat-count {
  @apply text-sm font-medium;
  color: #111827;
}

.dark .stat-count {
  color: #f9fafb;
}

.knowledge-list {
  @apply flex flex-col gap-3;
}

.knowledge-item {
  @apply flex flex-col gap-1;
}

.knowledge-header {
  @apply flex items-center justify-between;
}

.knowledge-name {
  @apply text-sm;
  color: #374151;
}

.dark .knowledge-name {
  color: #d1d5db;
}

.knowledge-percent {
  @apply text-sm font-medium;
  color: #111827;
}

.dark .knowledge-percent {
  color: #f9fafb;
}

.suggestions {
  @apply px-4 py-4 bg-[#fef3c7] border border-[#fcd34d] rounded-md;
}

.dark .suggestions {
  background: #451a0333;
  border-color: #b45309;
}

.suggestions-title {
  @apply text-sm font-semibold mb-2 m-0;
  color: #92400e;
}

.dark .suggestions-title {
  color: #fcd34d;
}

.suggestions-list {
  @apply text-sm m-0 pl-5;
  color: #78350f;
}

.dark .suggestions-list {
  color: #fde68a;
}

.suggestions-list li {
  @apply mb-1;
}

/* 滚动条 */
.details-list::-webkit-scrollbar {
  width: 6px;
}

.details-list::-webkit-scrollbar-thumb {
  background: rgba(156, 163, 175, 0.5);
  border-radius: 3px;
}

.details-list::-webkit-scrollbar-thumb:hover {
  background: rgba(156, 163, 175, 0.7);
}
</style>
