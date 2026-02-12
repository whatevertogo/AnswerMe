<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  ArrowLeft,
  TrendCharts,
  Check,
  Close,
  Clock,
  Document,
  MagicStick,
  Search,
  Download,
  CopyDocument,
  RefreshRight
} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getQuizResult, getQuizDetails, generateAttemptAISuggestions } from '@/api/quiz'
import type { QuizResult, QuizDetail, AttemptAISuggestion } from '@/api/quiz'
import { extractErrorMessage } from '@/utils/errorHandler'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const result = ref<QuizResult | null>(null)
const details = ref<QuizDetail[]>([])
const generatingSuggestion = ref(false)
const suggestion = ref<AttemptAISuggestion | null>(null)
const suggestionError = ref('')
const detailFilter = ref<'all' | 'incorrect' | 'unanswered' | 'slow'>('all')
const detailSearchKeyword = ref('')

const sessionId = computed(() => route.params.sessionId as string)
const attemptId = computed(() => parseInt(sessionId.value))

const hasUserAnswer = (detail: QuizDetail): boolean => {
  if (Array.isArray(detail.userAnswer)) {
    return detail.userAnswer.length > 0
  }
  return typeof detail.userAnswer === 'string' && detail.userAnswer.trim().length > 0
}

const hasAnsweredString = (answer: string | string[] | undefined): boolean => {
  if (Array.isArray(answer)) {
    return answer.length > 0
  }
  return typeof answer === 'string' && answer.trim().length > 0
}

// 统计数据
const correctCount = computed(() => details.value.filter(d => d.isCorrect).length)
const answeredCount = computed(() => details.value.filter(hasUserAnswer).length)
const incorrectCount = computed(() => Math.max(answeredCount.value - correctCount.value, 0))
const unansweredCount = computed(() =>
  Math.max((result.value?.totalQuestions || 0) - answeredCount.value, 0)
)
const accuracyRate = computed(() => {
  const total = result.value?.totalQuestions || 0
  if (!total) return 0
  return Number(((correctCount.value / total) * 100).toFixed(2))
})
const averageTimePerAnswered = computed(() => {
  const timeValues = details.value
    .filter(
      detail =>
        hasUserAnswer(detail) && typeof detail.timeSpent === 'number' && detail.timeSpent > 0
    )
    .map(detail => detail.timeSpent as number)

  if (!timeValues.length) return 0
  return Math.round(timeValues.reduce((sum, sec) => sum + sec, 0) / timeValues.length)
})
const slowQuestionCount = computed(
  () =>
    details.value.filter(
      detail =>
        hasUserAnswer(detail) &&
        typeof detail.timeSpent === 'number' &&
        (detail.timeSpent as number) > 30
    ).length
)

const normalizedDetailSearch = computed(() => detailSearchKeyword.value.trim().toLowerCase())

const filteredDetails = computed(() => {
  return details.value.filter(detail => {
    if (
      detailFilter.value === 'incorrect' &&
      (!hasAnsweredString(detail.userAnswer) || detail.isCorrect !== false)
    ) {
      return false
    }

    if (detailFilter.value === 'unanswered' && hasAnsweredString(detail.userAnswer)) {
      return false
    }

    if (
      detailFilter.value === 'slow' &&
      (!hasAnsweredString(detail.userAnswer) ||
        typeof detail.timeSpent !== 'number' ||
        detail.timeSpent <= 30)
    ) {
      return false
    }

    if (!normalizedDetailSearch.value) {
      return true
    }

    const questionText = detail.questionText?.toLowerCase() || ''
    const userAnswer = formatAnswer(detail.userAnswer).toLowerCase()
    const correctAnswer = formatAnswer(detail.correctAnswer).toLowerCase()

    return (
      questionText.includes(normalizedDetailSearch.value) ||
      userAnswer.includes(normalizedDetailSearch.value) ||
      correctAnswer.includes(normalizedDetailSearch.value)
    )
  })
})

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
  if (!hasAnsweredString(detail.userAnswer)) {
    return 'unanswered'
  }
  return detail.isCorrect ? 'correct' : 'incorrect'
}

async function generateAISuggestions() {
  if (generatingSuggestion.value) return

  try {
    generatingSuggestion.value = true
    suggestionError.value = ''
    suggestion.value = null
    suggestion.value = await generateAttemptAISuggestions(attemptId.value)
    ElMessage.success('AI 建议生成成功')
  } catch (error: unknown) {
    const message = extractErrorMessage(error, '生成 AI 建议失败')
    suggestionError.value = message
    ElMessage.error(message)
  } finally {
    generatingSuggestion.value = false
  }
}

async function copySuggestionText() {
  if (!suggestion.value) return

  const suggestionText = buildSuggestionText(suggestion.value)
  if (!suggestionText.trim()) return

  try {
    await navigator.clipboard.writeText(suggestionText)
    ElMessage.success('AI 建议已复制')
  } catch {
    ElMessage.error('复制失败，请手动复制')
  }
}

function exportSuggestionText() {
  if (!suggestion.value) return

  const content = buildSuggestionText(suggestion.value)
  if (!content.trim()) return

  const fileName = `ai_suggestion_attempt_${attemptId.value}.txt`
  downloadTextFile(content, fileName)
  ElMessage.success('AI 建议已导出')
}

function exportResultReport() {
  if (!result.value) return

  const lines: string[] = [
    'AnswerMe 答题复盘报告',
    '====================',
    `题库：${result.value.questionBankName || '未知'}`,
    `尝试ID：${result.value.id}`,
    `完成时间：${formatDate(result.value.completedAt)}`,
    `总题数：${result.value.totalQuestions}`,
    `答对：${correctCount.value}`,
    `答错：${incorrectCount.value}`,
    `未作答：${unansweredCount.value}`,
    `正确率：${accuracyRate.value}%`,
    `总用时：${formatDuration(result.value.durationSeconds || 0)}`,
    `平均每题用时：${averageTimePerAnswered.value} 秒`,
    ''
  ]

  if (suggestion.value) {
    lines.push('AI 学习建议')
    lines.push('--------------------')
    lines.push(buildSuggestionText(suggestion.value))
    lines.push('')
  }

  lines.push('题目详情')
  lines.push('--------------------')

  details.value.forEach((detail, index) => {
    const status = hasAnsweredString(detail.userAnswer)
      ? detail.isCorrect
        ? '正确'
        : '错误'
      : '未作答'
    lines.push(`第 ${index + 1} 题 [${status}]`)
    lines.push(`题干：${detail.questionText}`)
    lines.push(`你的答案：${formatAnswer(detail.userAnswer)}`)
    lines.push(`正确答案：${formatAnswer(detail.correctAnswer)}`)
    if (detail.explanation) {
      lines.push(`解析：${detail.explanation}`)
    }
    lines.push('')
  })

  downloadTextFile(lines.join('\n'), `result_report_attempt_${attemptId.value}.txt`)
  ElMessage.success('复盘报告已导出')
}

function buildSuggestionText(data: AttemptAISuggestion): string {
  const lines: string[] = [
    `AI模型：${data.providerName}`,
    `数据源：${data.dataSourceName}`,
    `生成时间：${formatDate(data.generatedAt)}`,
    '',
    '总体诊断',
    data.summary,
    '',
    '行动建议'
  ]

  data.suggestions.forEach((item, index) => {
    lines.push(`${index + 1}. ${item}`)
  })

  lines.push('')
  lines.push('7天复习计划')
  lines.push(data.studyPlan)

  if (data.weakPoints.length > 0) {
    lines.push('')
    lines.push('薄弱点')
    data.weakPoints.forEach(point => {
      lines.push(
        `- ${point.category}: ${point.name} (错误 ${point.incorrect}/${point.total}, 准确率 ${point.accuracyRate}%)`
      )
    })
  }

  return lines.join('\n')
}

function downloadTextFile(content: string, fileName: string) {
  const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  URL.revokeObjectURL(url)
}

function resetDetailFilters() {
  detailFilter.value = 'all'
  detailSearchKeyword.value = ''
}

// 加载结果数据
async function loadResult() {
  try {
    loading.value = true

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
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="8" animated />
      <p class="loading-text">正在加载结果...</p>
    </div>

    <template v-else-if="result">
      <div class="result-header">
        <el-button :icon="ArrowLeft" @click="goBack">返回练习</el-button>
        <h2 class="header-title">答题结果</h2>
        <div class="header-actions">
          <el-button :icon="Download" @click="exportResultReport">导出复盘</el-button>
          <el-button type="primary" @click="retryPractice">重新练习</el-button>
        </div>
      </div>

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
            <span class="info-label">总用时</span>
            <span class="info-value">
              <el-icon><Clock /></el-icon>
              {{ formatDuration(result.durationSeconds || 0) }}
            </span>
          </div>
        </div>
      </div>

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

      <div class="summary-row">
        <div class="summary-item">
          <span class="summary-label">正确率</span>
          <span class="summary-value">{{ accuracyRate }}%</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">平均每题用时</span>
          <span class="summary-value">{{ averageTimePerAnswered }} 秒</span>
        </div>
        <div class="summary-item">
          <span class="summary-label">高耗时题(>30秒)</span>
          <span class="summary-value">{{ slowQuestionCount }} 题</span>
        </div>
      </div>

      <div class="ai-section">
        <div class="section-header">
          <h3 class="section-title">AI 学习建议</h3>
          <div class="ai-actions">
            <el-button
              v-if="suggestion"
              :icon="CopyDocument"
              :disabled="generatingSuggestion"
              @click="copySuggestionText"
            >
              复制建议
            </el-button>
            <el-button
              v-if="suggestion"
              :icon="Download"
              :disabled="generatingSuggestion"
              @click="exportSuggestionText"
            >
              导出建议
            </el-button>
            <el-button
              type="primary"
              :icon="MagicStick"
              :loading="generatingSuggestion"
              @click="generateAISuggestions"
            >
              {{ suggestion ? '重新生成建议' : '生成 AI 建议' }}
            </el-button>
          </div>
        </div>

        <p class="ai-hint">将使用你在「AI配置」中设置的默认数据源生成建议。</p>

        <el-alert
          v-if="suggestionError"
          type="error"
          :closable="false"
          class="ai-alert"
          :title="suggestionError"
        />

        <el-skeleton v-if="generatingSuggestion" :rows="5" animated class="ai-skeleton" />

        <el-empty v-else-if="!suggestion" description="尚未生成 AI 建议" :image-size="100" />

        <div v-else class="ai-content">
          <div class="ai-meta">
            <el-tag type="info" size="small">{{ suggestion.providerName }}</el-tag>
            <el-tag size="small">{{ suggestion.dataSourceName }}</el-tag>
            <span class="ai-generated-time">生成于 {{ formatDate(suggestion.generatedAt) }}</span>
          </div>

          <div class="ai-summary-card">
            <h4 class="ai-subtitle">总体诊断</h4>
            <p class="ai-summary-text">{{ suggestion.summary }}</p>
          </div>

          <div class="ai-suggestions-card">
            <h4 class="ai-subtitle">行动建议</h4>
            <ul class="ai-suggestion-list">
              <li v-for="(item, index) in suggestion.suggestions" :key="`suggestion-${index}`">
                {{ item }}
              </li>
            </ul>
          </div>

          <div class="ai-plan-card">
            <h4 class="ai-subtitle">7天复习计划</h4>
            <p class="ai-plan-text">{{ suggestion.studyPlan }}</p>
          </div>

          <div v-if="suggestion.weakPoints.length > 0" class="weak-point-card">
            <h4 class="ai-subtitle">薄弱点识别</h4>
            <div class="weak-point-list">
              <el-tag
                v-for="item in suggestion.weakPoints"
                :key="`${item.category}-${item.name}`"
                type="warning"
                class="weak-point-tag"
              >
                {{ item.category }}: {{ item.name }} ({{ item.incorrect }}/{{ item.total }})
              </el-tag>
            </div>
          </div>
        </div>
      </div>

      <div class="details-section">
        <div class="section-header">
          <h3 class="section-title">题目详情</h3>
          <span class="detail-count"
            >已显示 {{ filteredDetails.length }} / {{ details.length }} 题</span
          >
        </div>
        <div class="detail-toolbar">
          <el-radio-group v-model="detailFilter" size="small">
            <el-radio-button value="all">全部</el-radio-button>
            <el-radio-button value="incorrect">仅错题</el-radio-button>
            <el-radio-button value="unanswered">仅未作答</el-radio-button>
            <el-radio-button value="slow">仅高耗时</el-radio-button>
          </el-radio-group>
          <div class="detail-tools-right">
            <el-input
              v-model="detailSearchKeyword"
              :prefix-icon="Search"
              placeholder="搜索题干/答案"
              clearable
              class="detail-search"
            />
            <el-button :icon="RefreshRight" size="small" @click="resetDetailFilters">
              重置筛选
            </el-button>
          </div>
        </div>
        <el-card class="details-card">
          <el-empty
            v-if="filteredDetails.length === 0"
            description="当前筛选条件下没有匹配题目"
            :image-size="100"
          />
          <div
            v-for="(detail, index) in filteredDetails"
            :key="detail.questionId"
            :class="['detail-item', getAnswerClass(detail)]"
          >
            <div class="detail-header">
              <span class="question-number">第 {{ index + 1 }} 题</span>
              <el-tag v-if="detail.isCorrect" type="success" size="small"> 正确 </el-tag>
              <el-tag v-else-if="hasAnsweredString(detail.userAnswer)" type="danger" size="small">
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

.loading-container {
  @apply flex flex-col items-center gap-6 p-12;
}

.loading-text {
  @apply text-text-secondary text-base;
}

.result-header {
  @apply flex items-center justify-between mb-8 pb-4 border-b border-border;
}

.header-actions {
  @apply flex items-center gap-2;
}

.header-title {
  @apply text-[1.5rem] font-bold text-text-primary m-0;
}

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

.stats-row {
  @apply grid grid-cols-4 gap-4 mb-4;
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

.summary-row {
  @apply grid grid-cols-3 gap-4 mb-8;
}

.summary-item {
  @apply flex items-center justify-between px-4 py-3 rounded-lg border border-border bg-bg shadow-xs;
}

.summary-label {
  @apply text-sm text-text-secondary;
}

.summary-value {
  @apply text-base font-semibold text-text-primary;
}

.section-header {
  @apply flex items-center justify-between gap-3;
}

.ai-actions {
  @apply flex items-center gap-2 flex-wrap;
}

.section-title {
  @apply text-[1.125rem] font-semibold text-text-primary m-0 mb-4;
}

.ai-section {
  @apply mb-8 p-5 rounded-lg border border-border bg-bg shadow-xs;
}

.ai-hint {
  @apply text-sm text-text-secondary mt-0 mb-4;
}

.ai-alert {
  @apply mb-4;
}

.ai-skeleton {
  @apply mb-4;
}

.ai-content {
  @apply flex flex-col gap-4;
}

.ai-meta {
  @apply flex flex-wrap items-center gap-2;
}

.ai-generated-time {
  @apply text-xs text-text-muted ml-auto;
}

.ai-summary-card,
.ai-suggestions-card,
.ai-plan-card,
.weak-point-card {
  @apply p-4 rounded-md border border-border bg-bg-secondary;
}

.ai-subtitle {
  @apply m-0 mb-2 text-sm font-semibold text-text-primary;
}

.ai-summary-text,
.ai-plan-text {
  @apply m-0 text-sm text-text-primary leading-[1.7];
}

.ai-plan-text {
  white-space: pre-line;
}

.ai-suggestion-list {
  @apply m-0 pl-5 text-sm text-text-primary;
}

.ai-suggestion-list li {
  @apply mb-1 leading-[1.6];
}

.weak-point-list {
  @apply flex flex-wrap gap-2;
}

.weak-point-tag {
  @apply mr-0;
}

.details-section {
  @apply mb-8;
}

.detail-count {
  @apply text-sm text-text-secondary;
}

.detail-toolbar {
  @apply flex items-center justify-between gap-3 mb-4;
}

.detail-tools-right {
  @apply flex items-center gap-2;
}

.detail-search {
  @apply w-[280px];
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

  .summary-row {
    @apply grid-cols-1;
  }

  .result-header {
    @apply flex-col gap-4;
  }

  .header-actions {
    @apply w-full justify-between;
  }

  .section-header {
    @apply flex-col items-stretch;
  }

  .detail-toolbar {
    @apply flex-col items-stretch;
  }

  .detail-tools-right {
    @apply flex-col items-stretch;
  }

  .detail-search {
    @apply w-full;
  }

  .ai-generated-time {
    @apply ml-0;
  }
}
</style>
