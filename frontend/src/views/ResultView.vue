<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowLeft, TrendCharts, Check, Close, Clock, Document } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getQuizResult, getQuizDetails } from '@/api/quiz'
import type { QuizResult, QuizDetail } from '@/api/quiz'
import { extractErrorMessage } from '@/utils/errorHandler'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const result = ref<QuizResult | null>(null)
const details = ref<QuizDetail[]>([])

const sessionId = computed(() => route.params.sessionId as string)
const attemptId = computed(() => parseInt(sessionId.value))

// 统计数据
const correctCount = computed(() => details.value.filter(d => d.isCorrect).length)
const incorrectCount = computed(
  () => details.value.filter(d => !d.isCorrect && d.userAnswer).length
)
const unansweredCount = computed(
  () =>
    details.value.filter(
      d => !d.userAnswer || (Array.isArray(d.userAnswer) && d.userAnswer.length === 0)
    ).length
)

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
  } catch (error: unknown) {
    ElMessage.error('加载结果失败: ' + extractErrorMessage(error, '未知错误'))
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
              <el-tag v-if="detail.isCorrect" type="success" size="small"> 正确 </el-tag>
              <el-tag
                v-else-if="
                  detail.userAnswer &&
                  !(Array.isArray(detail.userAnswer) && detail.userAnswer.length === 0)
                "
                type="danger"
                size="small"
              >
                错误
              </el-tag>
              <el-tag v-else type="info" size="small">未作答</el-tag>
              <el-button type="primary" size="small" text @click="showAnswer(detail)">
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
  @apply max-w-[1000px] mx-auto p-6;
}

/* 加载状态 */
.loading-container {
  @apply flex flex-col items-center gap-6 p-12;
}

.loading-text {
  @apply text-text-secondary text-base;
}

/* 顶部导航 */
.result-header {
  @apply flex items-center justify-between mb-8 pb-4 border-b border-border;
}

.header-title {
  @apply text-[1.5rem] font-bold text-text-primary m-0;
}

/* 分数卡片 */
.score-card {
  @apply flex items-center gap-8 rounded-xl p-8 mb-8 text-white shadow-md;
  background: var(--color-primary);
}

.score-circle {
  @apply flex flex-col items-center justify-center w-[150px] h-[150px]
         rounded-full flex-shrink-0;
  background-color: rgba(255, 255, 255, 0.2);
}

.score-value {
  @apply text-[3rem] font-bold leading-none;
}

.score-label {
  @apply text-sm opacity-90 mt-2;
}

.score-info {
  @apply flex-1 flex flex-col gap-4;
}

.info-item {
  @apply flex justify-between items-center;
}

.info-label {
  @apply opacity-90 text-sm;
}

.info-value {
  @apply font-semibold flex items-center gap-2;
}

/* 统计数据 */
.stats-row {
  @apply grid grid-cols-4 gap-4 mb-8;
}

.stat-item {
  @apply flex items-center gap-4 px-5 rounded-lg bg-bg border border-border shadow-xs
         transition-all duration-300 ease-in-out
         hover:-translate-y-0.5 hover:shadow-sm;
}

.stat-item.correct {
  @apply border-success;
  background-color: var(--color-success-light);
  border-color: var(--color-success);
}

.stat-item.correct .el-icon {
  @apply text-success;
}

.stat-item.incorrect {
  @apply border-danger;
  background-color: var(--color-danger-light);
  border-color: var(--color-danger);
}

.stat-item.incorrect .el-icon {
  @apply text-danger;
}

.stat-item.unanswered {
  @apply border;
  background-color: var(--color-bg-secondary);
  border-color: var(--color-border);
}

.stat-item.unanswered .el-icon {
  @apply text-text-muted;
}

.stat-item.total {
  @apply border-info;
  background-color: var(--color-info-light);
  border-color: var(--color-info);
}

.stat-item.total .el-icon {
  @apply text-info;
}

.stat-content {
  @apply flex flex-col;
}

.stat-value {
  @apply text-[1.5rem] font-bold leading-none text-text-primary;
}

.stat-label {
  @apply text-xs text-text-secondary mt-1;
}

/* 详情区域 */
.details-section {
  @apply mb-8;
}

.section-title {
  @apply text-[1.125rem] font-semibold text-text-primary m-0 mb-4;
}

.details-card {
  @apply bg-bg rounded-lg border border-border shadow-xs;
}

.detail-item {
  @apply px-5 py-5 border-b border-border
         transition-all duration-300 ease-in-out
         hover:bg-bg-secondary;
}

.detail-item:last-child {
  @apply border-b-0;
}

.detail-item.correct {
  background-color: var(--color-success-light);
  border-left-width: 4px;
  border-left-style: solid;
  border-left-color: var(--color-success);
}

.detail-item.incorrect {
  background-color: var(--color-danger-light);
  border-left-width: 4px;
  border-left-style: solid;
  border-left-color: var(--color-danger);
}

.detail-item.unanswered {
  background-color: var(--color-bg-secondary);
  border-left-width: 4px;
  border-left-style: solid;
  border-left-color: var(--color-text-muted);
}

.detail-header {
  @apply flex items-center gap-3 mb-3;
}

.question-number {
  @apply font-semibold text-text-primary;
}

.detail-content {
  @apply pl-2;
}

.question-text {
  @apply text-text-primary mb-3 leading-[1.6];
}

.answer-row {
  @apply flex gap-2 mb-1;
}

.answer-label {
  @apply text-text-secondary text-sm flex-shrink-0;
}

.answer-value {
  @apply text-text-primary text-sm;
}

.answer-value.correct {
  @apply text-success font-medium;
}

.answer-value.incorrect {
  @apply text-danger font-medium;
}

.answer-value.unanswered {
  @apply text-text-muted italic;
}

.correct-answer {
  @apply text-success font-medium;
}

/* 弹窗内容 */
.answer-modal-content {
  @apply py-2;
}

.modal-question {
  @apply text-base text-text-primary leading-[1.6] mb-2;
}

.modal-info {
  @apply flex flex-col gap-3;
}

.info-row {
  @apply flex gap-3;
}

.info-row .label {
  @apply text-text-secondary flex-shrink-0;
}

.info-row .value {
  @apply text-text-primary font-medium;
}

.info-row .value.correct {
  @apply text-success;
}

.info-row .value.incorrect {
  @apply text-danger;
}

.explanation-section {
  @apply mt-6 p-4 rounded-md;
  background-color: var(--color-success-light);
  border: 1px solid var(--color-success);
}

.explanation-title {
  @apply m-0 mb-2 text-sm font-semibold;
  color: var(--color-success);
}

.explanation-text {
  @apply m-0 text-text-primary leading-[1.6] text-sm;
}

/* 响应式 */
@media (max-width: 768px) {
  .result-view {
    @apply p-4;
  }

  .score-card {
    @apply flex-col text-center;
  }

  .score-info {
    @apply w-full;
  }

  .stats-row {
    @apply grid-cols-2;
  }

  .result-header {
    @apply flex-col gap-4;
  }
}
</style>
