<script setup lang="ts">
import { ref, computed } from 'vue'
import { CircleCheck, Clock, Aim, RefreshRight } from '@element-plus/icons-vue'

interface Question {
  id: number
  content: string
  type: 'single' | 'multiple' | 'essay'
  difficulty: string
  tags: string[]
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
const correctCount = computed(() => Math.floor(answeredCount.value * 0.7)) // 模拟正确率
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
    case 'easy': return '#10b981'
    case 'medium': return '#f59e0b'
    case 'hard': return '#ef4444'
    default: return '#6b7280'
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
              <el-icon :size="20" color="#10b981"><TrendingUp /></el-icon>
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
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.score-card {
  padding: 1.5rem;
  border-radius: 0.75rem;
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
  padding: 1.5rem;
  border-radius: 0.75rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.card-label {
  font-size: 0.875rem;
  color: #6b7280;
}

.dark .card-label {
  color: #9ca3af;
}

.score-value {
  font-size: 2.25rem;
  font-weight: 700;
  line-height: 1;
  color: #111827;
  margin-bottom: 0.25rem;
}

.dark .score-value {
  color: #f9fafb;
}

.stat-value {
  font-size: 2.25rem;
  font-weight: 700;
  line-height: 1;
  color: #111827;
  margin-bottom: 0.25rem;
}

.dark .stat-value {
  color: #f9fafb;
}

.score-label,
.stat-label {
  font-size: 0.875rem;
  color: #6b7280;
}

.dark .score-label,
.dark .stat-label {
  color: #9ca3af;
}

.progress-section {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.progress-item {
  display: flex;
  flex-direction: column;
  gap: 0.375rem;
}

.progress-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.progress-label {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.dark .progress-label {
  color: #d1d5db;
}

.progress-value {
  font-size: 0.875rem;
  color: #6b7280;
}

.dark .progress-value {
  color: #9ca3af;
}

.action-buttons {
  display: flex;
  gap: 0.75rem;
}

/* 详情列表 */
.details-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  max-height: 500px;
  overflow-y: auto;
}

.detail-item {
  padding: 1rem;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  background: #ffffff;
}

.dark .detail-item {
  background: #1f2937;
  border-color: #374151;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.detail-status {
  font-size: 1rem;
}

.detail-status.answered {
  color: #10b981;
}

.detail-status.unanswered {
  color: #d1d5db;
}

.detail-index {
  font-size: 0.875rem;
  font-weight: 500;
  color: #6b7280;
}

.dark .detail-index {
  color: #9ca3af;
}

.detail-content {
  font-size: 0.875rem;
  color: #374151;
  margin: 0 0 0.5rem 0;
  line-height: 1.5;
}

.dark .detail-content {
  color: #d1d5db;
}

.detail-explanation {
  margin-top: 0.75rem;
  padding: 0.75rem;
  background: #dbeafe;
  border-radius: 0.5rem;
  border: 1px solid #93c5fd;
}

.dark .detail-explanation {
  background: #1e3a8a33;
  border-color: #1e40af;
}

.explanation-label {
  font-size: 0.75rem;
  font-weight: 600;
  color: #1e40af;
  margin: 0 0 0.25rem 0;
}

.dark .explanation-label {
  color: #60a5fa;
}

.explanation-text {
  font-size: 0.75rem;
  color: #1e3a8a;
  margin: 0;
}

.dark .explanation-text {
  color: #93c5fd;
}

/* 分析 */
.analysis-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
  margin-bottom: 1rem;
}

.analysis-card {
  padding: 1rem;
  background: #f9fafb;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
}

.dark .analysis-card {
  background: #1f2937;
  border-color: #374151;
}

.analysis-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #111827;
  margin: 0 0 0.75rem 0;
}

.dark .analysis-title {
  color: #f9fafb;
}

.stat-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.stat-dot {
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 9999px;
}

.stat-dot.easy {
  background: #10b981;
}

.stat-dot.medium {
  background: #f59e0b;
}

.stat-dot.hard {
  background: #ef4444;
}

.stat-name {
  font-size: 0.875rem;
  color: #374151;
  flex: 1;
}

.dark .stat-name {
  color: #d1d5db;
}

.stat-count {
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
}

.dark .stat-count {
  color: #f9fafb;
}

.knowledge-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.knowledge-item {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.knowledge-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.knowledge-name {
  font-size: 0.875rem;
  color: #374151;
}

.dark .knowledge-name {
  color: #d1d5db;
}

.knowledge-percent {
  font-size: 0.875rem;
  font-weight: 500;
  color: #111827;
}

.dark .knowledge-percent {
  color: #f9fafb;
}

.suggestions {
  padding: 1rem;
  background: #fef3c7;
  border: 1px solid #fcd34d;
  border-radius: 0.5rem;
}

.dark .suggestions {
  background: #451a0333;
  border-color: #b45309;
}

.suggestions-title {
  font-size: 0.875rem;
  font-weight: 600;
  color: #92400e;
  margin: 0 0 0.5rem 0;
}

.dark .suggestions-title {
  color: #fcd34d;
}

.suggestions-list {
  font-size: 0.875rem;
  color: #78350f;
  margin: 0;
  padding-left: 1.25rem;
}

.dark .suggestions-list {
  color: #fde68a;
}

.suggestions-list li {
  margin-bottom: 0.25rem;
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
