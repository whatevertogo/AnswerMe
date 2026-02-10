<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, TrendCharts, Check, Close, Clock, Document } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getQuizResult, getQuizDetails } from '@/api/quiz'
import type { QuizResult, QuizDetail } from '@/api/quiz'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const result = ref<QuizResult | null>(null)
const details = ref<QuizDetail[]>([])

const sessionId = computed(() => route.params.sessionId as string)
const attemptId = computed(() => parseInt(sessionId.value))

// 统计数据
const correctCount = computed(() => details.value.filter(d => d.isCorrect).length)
const incorrectCount = computed(() => details.value.filter(d => !d.isCorrect && d.userAnswer).length)
const unansweredCount = computed(() => details.value.filter(d => !d.userAnswer || (Array.isArray(d.userAnswer) && d.userAnswer.length === 0)).length)

// 格式化时间
const formatDuration = (seconds: number) => {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins} 分 ${secs} 秒`
}

// 格式化日期
const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// 获取答案显示文本
const formatAnswer = (answer: string | string[] | undefined): string => {
  if (!answer) return '未作答'
  if (Array.isArray(answer)) {
    return answer.join(', ')
  }
  return answer
}

// 判断答案是否正确（带样式）
const getAnswerClass = (detail: QuizDetail) => {
  if (!detail.userAnswer || (Array.isArray(detail.userAnswer) && detail.userAnswer.length === 0)) {
    return 'unanswered'
  }
  return detail.isCorrect ? 'correct' : 'incorrect'
}

// 加载结果数据
async function loadResult() {
  try {
    loading.value = true

    // 并行加载结果和详情
    const [resultRes, detailsRes] = await Promise.all([
      getQuizResult(attemptId.value),
      getQuizDetails(attemptId.value)
    ])

    result.value = resultRes
    details.value = detailsRes
  } catch (error: any) {
    ElMessage.error('加载结果失败: ' + (error.message || '未知错误'))
    console.error('加载结果失败:', error)
  } finally {
    loading.value = false
  }
}

// 返回练习页
const goBack = () => {
  router.push('/practice')
}

// 重新练习
const retryPractice = () => {
  if (result.value?.questionBankId) {
    router.push(`/quiz/${result.value.questionBankId}/new`)
  }
}

// 查看答案解析
const selectedQuestion = ref<QuizDetail | null>(null)
const showAnswerModal = ref(false)

const showAnswer = (detail: QuizDetail) => {
  selectedQuestion.value = detail
  showAnswerModal.value = true
}

onMounted(() => {
  loadResult()
})
</script>

<template>
  <div class="result-view">
    <!-- 加载状态 -->
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="8" animated />
      <p class="loading-text">正在加载结果...</p>
    </div>

    <template v-else-if="result">
      <!-- 顶部导航 -->
      <div class="result-header">
        <el-button :icon="ArrowLeft" @click="goBack">返回练习</el-button>
        <h2 class="header-title">答题结果</h2>
        <el-button type="primary" @click="retryPractice">重新练习</el-button>
      </div>

      <!-- 分数卡片 -->
      <div class="score-card">
        <div class="score-circle">
          <div class="score-value">{{ Math.round(result.score || 0) }}</div>
          <div class="score-label">得分</div>
        </div>
        <div class="score-info">
          <div class="info-item">
            <span class="info-label">题库</span>
            <span class="info-value">{{ result.questionBankName || '未知' }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">完成时间</span>
            <span class="info-value">{{ formatDate(result.completedAt) }}</span>
          </div>
          <div class="info-item">
            <span class="info-label">用时</span>
            <span class="info-value">
              <el-icon><Clock /></el-icon>
              {{ formatDuration(result.durationSeconds || 0) }}
            </span>
          </div>
        </div>
      </div>

      <!-- 统计数据 -->
      <div class="stats-row">
        <div class="stat-item correct">
          <el-icon :size="24"><Check /></el-icon>
          <div class="stat-content">
            <div class="stat-value">{{ correctCount }}</div>
            <div class="stat-label">答对</div>
          </div>
        </div>
        <div class="stat-item incorrect">
          <el-icon :size="24"><Close /></el-icon>
          <div class="stat-content">
            <div class="stat-value">{{ incorrectCount }}</div>
            <div class="stat-label">答错</div>
          </div>
        </div>
        <div class="stat-item unanswered">
          <el-icon :size="24"><Document /></el-icon>
          <div class="stat-content">
            <div class="stat-value">{{ unansweredCount }}</div>
            <div class="stat-label">未作答</div>
          </div>
        </div>
        <div class="stat-item total">
          <el-icon :size="24"><TrendCharts /></el-icon>
          <div class="stat-content">
            <div class="stat-value">{{ result.totalQuestions }}</div>
            <div class="stat-label">总题数</div>
          </div>
        </div>
      </div>

      <!-- 题目详情列表 -->
      <div class="details-section">
        <h3 class="section-title">题目详情</h3>
        <el-card class="details-card">
          <div
            v-for="(detail, index) in details"
            :key="detail.questionId"
            :class="['detail-item', getAnswerClass(detail)]"
          >
            <div class="detail-header">
              <span class="question-number">第 {{ index + 1 }} 题</span>
              <el-tag
                v-if="detail.isCorrect"
                type="success"
                size="small"
              >
                正确
              </el-tag>
              <el-tag
                v-else-if="detail.userAnswer && !(Array.isArray(detail.userAnswer) && detail.userAnswer.length === 0)"
                type="danger"
                size="small"
              >
                错误
              </el-tag>
              <el-tag v-else type="info" size="small">未作答</el-tag>
              <el-button
                type="primary"
                size="small"
                text
                @click="showAnswer(detail)"
              >
                查看解析
              </el-button>
            </div>
            <div class="detail-content">
              <div class="question-text">{{ detail.questionText }}</div>
              <div class="answer-row">
                <span class="answer-label">你的答案：</span>
                <span :class="['answer-value', getAnswerClass(detail)]">
                  {{ formatAnswer(detail.userAnswer) }}
                </span>
              </div>
              <div v-if="!detail.isCorrect" class="answer-row">
                <span class="answer-label">正确答案：</span>
                <span class="answer-value correct-answer">
                  {{ formatAnswer(detail.correctAnswer) }}
                </span>
              </div>
            </div>
          </div>
        </el-card>
      </div>
    </template>

    <!-- 答案解析弹窗 -->
    <el-dialog
      v-model="showAnswerModal"
      title="答案解析"
      width="600px"
      :close-on-click-modal="false"
    >
      <div v-if="selectedQuestion" class="answer-modal-content">
        <div class="modal-question">{{ selectedQuestion.questionText }}</div>
        <el-divider />
        <div class="modal-info">
          <div class="info-row">
            <span class="label">你的答案：</span>
            <span :class="['value', getAnswerClass(selectedQuestion)]">
              {{ formatAnswer(selectedQuestion.userAnswer) }}
            </span>
          </div>
          <div class="info-row">
            <span class="label">正确答案：</span>
            <span class="value correct-answer">
              {{ formatAnswer(selectedQuestion.correctAnswer) }}
            </span>
          </div>
          <div v-if="selectedQuestion.timeSpent" class="info-row">
            <span class="label">用时：</span>
            <span class="value">{{ selectedQuestion.timeSpent }} 秒</span>
          </div>
        </div>
        <div v-if="selectedQuestion.explanation" class="explanation-section">
          <h4 class="explanation-title">解析</h4>
          <p class="explanation-text">{{ selectedQuestion.explanation }}</p>
        </div>
      </div>
      <template #footer>
        <el-button @click="showAnswerModal = false">关闭</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.result-view {
  max-width: 1000px;
  margin: 0 auto;
  padding: 1.5rem;
}

/* 加载状态 */
.loading-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1.5rem;
  padding: 3rem;
}

.loading-text {
  color: #6b7280;
  font-size: 1rem;
}

/* 顶部导航 */
.result-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 2rem;
  padding-bottom: 1rem;
  border-bottom: 1px solid #e5e7eb;
}

.header-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #1f2937;
  margin: 0;
}

/* 分数卡片 */
.score-card {
  display: flex;
  align-items: center;
  gap: 2rem;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border-radius: 1rem;
  padding: 2rem;
  margin-bottom: 2rem;
  color: white;
}

.score-circle {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  width: 150px;
  height: 150px;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 50%;
  flex-shrink: 0;
}

.score-value {
  font-size: 3rem;
  font-weight: 700;
  line-height: 1;
}

.score-label {
  font-size: 0.875rem;
  opacity: 0.9;
  margin-top: 0.5rem;
}

.score-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.info-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.info-label {
  opacity: 0.9;
  font-size: 0.875rem;
}

.info-value {
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

/* 统计数据 */
.stats-row {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 1rem;
  margin-bottom: 2rem;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1.25rem;
  border-radius: 0.75rem;
  background: white;
  border: 1px solid #e5e7eb;
}

.stat-item.correct {
  border-color: #10b981;
  background: #f0fdf4;
}

.stat-item.correct .el-icon {
  color: #10b981;
}

.stat-item.incorrect {
  border-color: #ef4444;
  background: #fef2f2;
}

.stat-item.incorrect .el-icon {
  color: #ef4444;
}

.stat-item.unanswered {
  border-color: #9ca3af;
  background: #f9fafb;
}

.stat-item.unanswered .el-icon {
  color: #9ca3af;
}

.stat-item.total {
  border-color: #3b82f6;
  background: #eff6ff;
}

.stat-item.total .el-icon {
  color: #3b82f6;
}

.stat-content {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 1.5rem;
  font-weight: 700;
  line-height: 1;
}

.stat-label {
  font-size: 0.75rem;
  color: #6b7280;
  margin-top: 0.25rem;
}

/* 详情区域 */
.details-section {
  margin-bottom: 2rem;
}

.section-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #1f2937;
  margin: 0 0 1rem;
}

.details-card {
  background: white;
}

.detail-item {
  padding: 1.25rem;
  border-bottom: 1px solid #e5e7eb;
  transition: background 0.15s;
}

.detail-item:last-child {
  border-bottom: none;
}

.detail-item:hover {
  background: #f9fafb;
}

.detail-item.correct {
  background: #f0fdf4;
  border-left: 4px solid #10b981;
}

.detail-item.incorrect {
  background: #fef2f2;
  border-left: 4px solid #ef4444;
}

.detail-item.unanswered {
  background: #f9fafb;
  border-left: 4px solid #9ca3af;
}

.detail-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.question-number {
  font-weight: 600;
  color: #374151;
}

.detail-content {
  padding-left: 0.5rem;
}

.question-text {
  color: #4b5563;
  margin-bottom: 0.75rem;
  line-height: 1.6;
}

.answer-row {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.answer-label {
  color: #6b7280;
  font-size: 0.875rem;
  flex-shrink: 0;
}

.answer-value {
  color: #374151;
  font-size: 0.875rem;
}

.answer-value.correct {
  color: #10b981;
  font-weight: 500;
}

.answer-value.incorrect {
  color: #ef4444;
  font-weight: 500;
}

.answer-value.unanswered {
  color: #9ca3af;
  font-style: italic;
}

.correct-answer {
  color: #10b981;
  font-weight: 500;
}

/* 弹窗内容 */
.answer-modal-content {
  padding: 0.5rem 0;
}

.modal-question {
  font-size: 1rem;
  color: #374151;
  line-height: 1.6;
  margin-bottom: 0.5rem;
}

.modal-info {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.info-row {
  display: flex;
  gap: 0.75rem;
}

.info-row .label {
  color: #6b7280;
  flex-shrink: 0;
}

.info-row .value {
  color: #374151;
  font-weight: 500;
}

.info-row .value.correct {
  color: #10b981;
}

.info-row .value.incorrect {
  color: #ef4444;
}

.explanation-section {
  margin-top: 1.5rem;
  padding: 1rem;
  background: #f0fdf4;
  border-radius: 0.5rem;
}

.explanation-title {
  margin: 0 0 0.5rem;
  color: #065f46;
  font-size: 0.875rem;
  font-weight: 600;
}

.explanation-text {
  margin: 0;
  color: #047857;
  line-height: 1.6;
  font-size: 0.875rem;
}

/* 响应式 */
@media (max-width: 768px) {
  .result-view {
    padding: 1rem;
  }

  .score-card {
    flex-direction: column;
    text-align: center;
  }

  .score-info {
    width: 100%;
  }

  .stats-row {
    grid-template-columns: repeat(2, 1fr);
  }

  .result-header {
    flex-direction: column;
    gap: 1rem;
  }
}
</style>
