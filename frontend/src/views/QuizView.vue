<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  Clock,
  CircleCheck,
  Flag,
  ArrowLeft,
  ArrowRight,
  House,
  Reading,
  Sunny,
  Moon
} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import QuizQuestionList from '@/components/quiz/QuizQuestionList.vue'
import QuizQuestionPanel from '@/components/quiz/QuizQuestionPanel.vue'
import QuizAnswerPanel from '@/components/quiz/QuizAnswerPanel.vue'
import { useQuizStore } from '@/stores/quiz'
import { useTheme } from '@/composables/useTheme'
import { getQuestionDetail } from '@/api/question'
import type { QuizQuestion } from '@/stores/quiz'
import type { QuizDetail } from '@/api/quiz'
import type { Question } from '@/types/question'
import { getQuestionOptions, getQuestionCorrectAnswers } from '@/types/question'
import { extractErrorMessage } from '@/utils/errorHandler'
import { parseAnswerToArray } from '@/utils/answerFormatter'
import {
  compareChoiceAnswers,
  inferChoiceAnswerMode,
  mapChoiceAnswerForDisplay,
  mapChoiceAnswerForSubmit
} from '@/utils/quizAnswer'

const router = useRouter()
const route = useRoute()
const quizStore = useQuizStore()
const { isDark, toggleTheme } = useTheme()

// 从路由参数获取 bankId 和 attemptId (sessionId)
const bankId = computed(() => parseInt(route.params.bankId as string))
// 判断是否为新答题会话（根据路由名称或 sessionId 参数）
const isNewQuiz = computed(() => route.name === 'QuizNew' || route.params.sessionId === 'new')
const sessionId = computed(() => route.params.sessionId as string)

// 本地状态
const questions = ref<QuizQuestion[]>([])
const questionMap = ref<Map<number, QuizQuestion>>(new Map())
const markedQuestions = ref<Set<number>>(new Set())
const timeElapsed = ref(0)
const loading = ref(true)
const initializing = ref(true)

// localStorage key 用于保存标记状态
const STORAGE_KEY_MARKED = 'quiz_marked_questions'
const QUESTION_DETAIL_CONCURRENCY = 8

// 计时器
let timer: number | null = null

// Computed
const currentQuestion = computed(() => {
  const qid = quizStore.currentQuestionId
  if (qid === null || qid === undefined) return null
  return questionMap.value.get(qid) || null
})

const safeCurrentQuestion = computed(() => {
  const q = currentQuestion.value
  if (!q) {
    return {
      id: 0,
      content: '加载中...',
      type: 'single' as const,
      options: [],
      difficulty: 'medium' as const,
      tags: [],
      explanation: ''
    }
  }
  return q
})

const answeredCount = computed(() => quizStore.answeredCount)
const progress = computed(() => quizStore.progress)
const totalQuestions = computed(() => quizStore.questionIds.length)

// 初始化答题
onMounted(async () => {
  await initializeQuiz()
  startTimer()
})

onUnmounted(() => {
  stopTimer()
})

// 监听路由参数变化
watch(
  () => route.params,
  async () => {
    if (route.name === 'Quiz' || route.name === 'QuizNew') {
      stopTimer()
      await initializeQuiz()
      startTimer()
    }
  }
)

async function initializeQuiz() {
  initializing.value = true
  loading.value = true
  timeElapsed.value = 0
  questions.value = []
  questionMap.value = new Map()
  markedQuestions.value = new Set()
  quizStore.reset()

  try {
    // 如果是新答题路由或 sessionId 是 'new'，则开始新答题
    if (isNewQuiz.value) {
      // 开始新答题
      await startNewQuiz()
    } else {
      // 加载已有答题
      await loadExistingQuiz()
    }
  } catch (error: unknown) {
    ElMessage.error('初始化答题失败: ' + extractErrorMessage(error, '未知错误'))
    console.error('初始化答题失败:', error)
    router.back()
  } finally {
    initializing.value = false
    loading.value = false
  }
}

async function startNewQuiz() {
  // 调用 startQuiz API
  await quizStore.startQuiz(bankId.value, 'sequential')

  // 获取题目详情
  await loadQuestionsDetails(quizStore.questionIds)

  // 恢复标记状态（如果有保存的）
  loadMarkedQuestions()

  ElMessage.success('答题开始！')
}

async function loadExistingQuiz() {
  const attemptId = parseInt(sessionId.value)
  if (Number.isNaN(attemptId) || attemptId <= 0) {
    throw new Error('答题会话 ID 无效')
  }

  // 先获取答题会话状态，避免已完成会话继续提交导致 400
  const attemptResult = await quizStore.fetchQuizResult(attemptId)
  if (attemptResult.completedAt) {
    await router.replace(`/result/${attemptId}`)
    return
  }

  // 加载答题详情
  const details = await quizStore.fetchQuizDetails(attemptId)

  // 从详情中提取题目ID
  const questionIds = [...new Set(details.map((detail: QuizDetail) => detail.questionId))].filter(
    id => id > 0
  )

  // 更新 store 状态
  quizStore.currentAttemptId = attemptId
  quizStore.questionIds = questionIds
  quizStore.answers = {}
  quizStore.timeSpents = {}

  if (questionIds.length > 0) {
    // 获取题目详情
    await loadQuestionsDetails(questionIds)

    // 恢复答案和用时
    details.forEach((detail: QuizDetail) => {
      const question = questionMap.value.get(detail.questionId)
      if (typeof detail.userAnswer === 'string' && question) {
        const restoredAnswer = restoreAnswerForDisplay(question, detail.userAnswer)
        if (restoredAnswer !== undefined) {
          quizStore.answers[detail.questionId] = restoredAnswer
        }
      }
      if (typeof detail.timeSpent === 'number' && detail.timeSpent > 0) {
        quizStore.timeSpents[detail.questionId] = detail.timeSpent
      }
    })
  }

  // 恢复标记状态
  loadMarkedQuestions()
}

async function loadQuestionsDetails(questionIds: number[]) {
  const uniqueQuestionIds = [...new Set(questionIds)].filter(id => id > 0)
  if (!uniqueQuestionIds.length) {
    questions.value = []
    questionMap.value = new Map()
    return
  }

  try {
    // 限流并发获取题目详情，避免一次性并发过高导致整体失败
    const questionDetails: Array<Question | undefined> = new Array(uniqueQuestionIds.length)
    const failedQuestionIds: number[] = []
    const workerCount = Math.min(QUESTION_DETAIL_CONCURRENCY, uniqueQuestionIds.length)
    let nextIndex = 0

    const workers = Array.from({ length: workerCount }, () =>
      (async () => {
        while (nextIndex < uniqueQuestionIds.length) {
          const currentIndex = nextIndex++
          if (currentIndex >= uniqueQuestionIds.length) {
            return
          }

          const questionId = uniqueQuestionIds[currentIndex]
          if (!questionId) {
            continue
          }

          try {
            questionDetails[currentIndex] = await getQuestionDetail(questionId)
          } catch (error) {
            failedQuestionIds.push(questionId)
            console.error('加载题目详情失败:', { questionId, error })
          }
        }
      })()
    )

    await Promise.all(workers)

    const loadedDetails = questionDetails.filter((q): q is Question => q != null)
    if (!loadedDetails.length) {
      throw new Error('题目详情全部加载失败')
    }

    if (failedQuestionIds.length > 0) {
      ElMessage.warning(`部分题目加载失败（${failedQuestionIds.length}题），可刷新重试`)
    }

    // 转换为 QuizQuestion 格式
    questions.value = loadedDetails.map((q: Question) => {
      return {
        id: q.id,
        content: q.questionText,
        type: convertQuestionType(q.questionTypeEnum),
        options: extractOptions(q),
        difficulty: q.difficulty || 'medium',
        tags: q.tags || [],
        correctAnswer: extractCorrectAnswer(q),
        explanation: q.explanation
      }
    })

    // 创建映射
    questionMap.value = new Map(questions.value.map(q => [q.id, q]))

    // 与实际可用题目保持一致，避免缺失题目导致导航到空题
    const loadedQuestionIds = questions.value.map(q => q.id)
    quizStore.questionIds = loadedQuestionIds
    if (quizStore.currentQuestionIndex >= loadedQuestionIds.length) {
      quizStore.currentQuestionIndex = Math.max(loadedQuestionIds.length - 1, 0)
    }
  } catch (error: unknown) {
    ElMessage.error('加载题目详情失败: ' + extractErrorMessage(error, '未知错误'))
    throw error
  }
}

// 从新格式 data 中提取选项
function extractOptions(question: Question): string[] {
  return getQuestionOptions(question)
}

// 从新格式 data 中提取答案
function extractCorrectAnswer(question: Question): string | undefined {
  const answer = getQuestionCorrectAnswers(question)
  if (Array.isArray(answer)) {
    return answer.join(',')
  }
  if (typeof answer === 'boolean') {
    return answer ? 'true' : 'false'
  }
  return answer ? String(answer) : undefined
}

function convertQuestionType(type: string | undefined): QuizQuestion['type'] {
  const typeMap: Record<string, QuizQuestion['type']> = {
    SingleChoice: 'single',
    MultipleChoice: 'multiple',
    TrueFalse: 'boolean',
    FillBlank: 'fill',
    ShortAnswer: 'essay',
    // 兼容旧格式
    choice: 'single',
    'multiple-choice': 'multiple',
    'true-false': 'boolean',
    'short-answer': 'essay'
  }
  if (!type) {
    return 'single'
  }
  return typeMap[type] || 'single'
}

function isChoiceType(type: QuizQuestion['type']): boolean {
  return type === 'single' || type === 'multiple'
}

function restoreAnswerForDisplay(
  question: QuizQuestion,
  rawAnswer: string
): string | string[] | undefined {
  const normalizedRawAnswer = normalizeAnswerString(rawAnswer)
  if (!normalizedRawAnswer) {
    return undefined
  }

  if (!isChoiceType(question.type)) {
    return normalizedRawAnswer
  }

  const multiple = question.type === 'multiple'
  return mapChoiceAnswerForDisplay(normalizedRawAnswer, question.options, multiple)
}

function normalizeAnswerString(rawAnswer: string): string {
  const trimmed = rawAnswer.trim()
  if (!trimmed) {
    return ''
  }

  if (trimmed.startsWith('[') && trimmed.endsWith(']')) {
    try {
      const parsed = JSON.parse(trimmed)
      if (Array.isArray(parsed)) {
        return parsed
          .map(item => String(item).trim())
          .filter(Boolean)
          .join(',')
      }
    } catch {
      // 保持原值，交由后续展示/解析逻辑处理
    }
  }

  return trimmed
}

function mapAnswerForSubmit(question: QuizQuestion, answer: string | string[]): string | string[] {
  if (!isChoiceType(question.type)) {
    return answer
  }

  const mode = inferChoiceAnswerMode(question.correctAnswer, question.options)
  const multiple = question.type === 'multiple'
  return mapChoiceAnswerForSubmit(answer, question.options, mode, multiple)
}

function startTimer() {
  timer = window.setInterval(() => {
    timeElapsed.value++
  }, 1000)
}

function stopTimer() {
  if (timer) {
    clearInterval(timer)
    timer = null
  }
}

// Methods
const formatTime = (seconds: number) => {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
}

const handleAnswer = async (questionId: number, answer: string | string[]) => {
  if (questionId <= 0 || !quizStore.questionIds.includes(questionId)) {
    return
  }

  try {
    const question = questionMap.value.get(questionId)
    if (!question) {
      return
    }

    const normalizedAnswer =
      question.type === 'multiple'
        ? Array.from(new Set(parseAnswerToArray(answer).filter(Boolean)))
        : answer

    // 本地答案和提交口径分离：UI 用选项文本，提交按题目口径映射为 A/B 或文本
    const submitPayload = mapAnswerForSubmit(question, normalizedAnswer)

    // 选择题按映射后的语义去重，避免文本/A-B 形式不同导致重复请求
    if (
      isChoiceType(question.type) &&
      compareChoiceAnswers(quizStore.answers[questionId], submitPayload, question.options)
    ) {
      return
    }

    await quizStore.submitAnswer(questionId, normalizedAnswer, timeElapsed.value, submitPayload)
  } catch (error: unknown) {
    ElMessage.error('提交答案失败: ' + extractErrorMessage(error, '未知错误'))
    console.error('提交答案失败:', error)
  }
}

const toggleMark = (questionId: number) => {
  if (markedQuestions.value.has(questionId)) {
    markedQuestions.value.delete(questionId)
  } else {
    markedQuestions.value.add(questionId)
  }
  // 保存到 localStorage
  saveMarkedQuestions()
}

// 保存标记状态到 localStorage
const saveMarkedQuestions = () => {
  if (quizStore.currentAttemptId) {
    const key = `${STORAGE_KEY_MARKED}_${quizStore.currentAttemptId}`
    try {
      localStorage.setItem(key, JSON.stringify([...markedQuestions.value]))
    } catch {
      // 本地存储不可用时降级为内存状态，不阻断答题流程
    }
  }
}

// 从 localStorage 恢复标记状态
const loadMarkedQuestions = () => {
  if (quizStore.currentAttemptId) {
    const key = `${STORAGE_KEY_MARKED}_${quizStore.currentAttemptId}`
    try {
      const saved = localStorage.getItem(key)
      if (saved) {
        try {
          markedQuestions.value = new Set(JSON.parse(saved))
        } catch {
          markedQuestions.value = new Set()
        }
      }
    } catch {
      markedQuestions.value = new Set()
    }
  }
}

// 清除 localStorage 中的标记状态
const clearMarkedQuestions = () => {
  if (quizStore.currentAttemptId) {
    const key = `${STORAGE_KEY_MARKED}_${quizStore.currentAttemptId}`
    try {
      localStorage.removeItem(key)
    } catch {
      // 忽略本地存储异常，避免影响提交流程
    }
  }
}

const goToQuestion = (index: number) => {
  quizStore.goToQuestion(index)
}

const goToNext = () => {
  quizStore.goToNext()
}

const goToPrevious = () => {
  quizStore.goToPrevious()
}

const handleSubmit = async () => {
  try {
    loading.value = true
    const completed = await quizStore.completeQuiz()
    // 清除标记状态
    clearMarkedQuestions()
    ElMessage.success('交卷成功！')
    await router.push(`/result/${completed.id}`)
  } catch (error: unknown) {
    ElMessage.error('交卷失败: ' + extractErrorMessage(error, '未知错误'))
    console.error('交卷失败:', error)
  } finally {
    loading.value = false
  }
}

const goHome = () => {
  router.push('/home')
}
</script>

<template>
  <div class="quiz-container">
    <!-- 加载状态 -->
    <div v-if="initializing" class="loading-container">
      <el-skeleton :rows="5" animated />
      <p class="loading-text">正在初始化答题...</p>
    </div>

    <template v-else-if="questions.length > 0">
      <!-- 顶部导航栏 -->
      <header class="quiz-header">
        <div class="header-left">
          <div class="logo-section">
            <el-icon :size="20" class="logo-icon"><Reading /></el-icon>
            <h1 class="logo-title">AnswerMe</h1>
          </div>
          <el-divider direction="vertical" />
          <div class="quiz-info">
            <span>题库 #{{ bankId }}</span>
            <span class="separator">·</span>
            <span>{{ totalQuestions }} 题</span>
          </div>
        </div>

        <div class="header-right">
          <!-- 进度 -->
          <div class="progress-indicator">
            <el-icon :size="16" color="var(--color-success)"><CircleCheck /></el-icon>
            <span class="progress-text">{{ answeredCount }}/{{ totalQuestions }}</span>
            <el-progress :percentage="progress" :show-text="false" :stroke-width="6" />
          </div>

          <!-- 计时器 -->
          <div class="timer-indicator">
            <el-icon :size="16" color="var(--color-primary)"><Clock /></el-icon>
            <span class="timer-text">{{ formatTime(timeElapsed) }}</span>
          </div>

          <!-- 深色模式切换 -->
          <el-button :icon="isDark ? Sunny : Moon" circle size="small" @click="toggleTheme" />

          <el-button @click="goHome">
            <el-icon><House /></el-icon>
            返回首页
          </el-button>
        </div>
      </header>

      <!-- 主体内容 -->
      <div class="quiz-body">
        <!-- 左侧题目列表 -->
        <QuizQuestionList
          :questions="questions"
          :current-index="quizStore.currentQuestionIndex"
          :answers="quizStore.answers"
          :marked-questions="markedQuestions"
          @question-click="goToQuestion"
        />

        <!-- 中间题目面板 -->
        <div class="quiz-main">
          <QuizQuestionPanel
            :question="safeCurrentQuestion"
            :question-number="quizStore.currentQuestionIndex + 1"
            :total-questions="totalQuestions"
          />

          <!-- 答题区域 -->
          <div class="answer-section">
            <QuizAnswerPanel
              :question="safeCurrentQuestion"
              :answer="quizStore.answers[safeCurrentQuestion.id]"
              :disabled="quizStore.loading"
              @update:answer="ans => handleAnswer(safeCurrentQuestion.id, ans)"
            />
          </div>
        </div>
      </div>

      <!-- 底部操作栏 -->
      <footer class="quiz-footer">
        <div class="footer-left">
          <el-button
            :type="markedQuestions.has(safeCurrentQuestion.id) ? 'warning' : 'default'"
            :icon="Flag"
            @click="toggleMark(safeCurrentQuestion.id)"
          >
            {{ markedQuestions.has(safeCurrentQuestion.id) ? '已标记' : '标记' }}
          </el-button>
        </div>

        <div class="footer-right">
          <el-button
            :icon="ArrowLeft"
            :disabled="quizStore.currentQuestionIndex === 0"
            @click="goToPrevious"
          >
            上一题
          </el-button>

          <el-button
            :icon="ArrowRight"
            :disabled="quizStore.currentQuestionIndex >= totalQuestions - 1"
            @click="goToNext"
          >
            下一题
          </el-button>

          <el-button
            type="primary"
            :disabled="answeredCount === 0"
            :loading="loading"
            @click="handleSubmit"
          >
            交卷
          </el-button>
        </div>
      </footer>
    </template>

    <!-- 空状态 -->
    <div v-else class="empty-container">
      <el-empty description="暂无题目">
        <el-button type="primary" @click="goHome">返回首页</el-button>
      </el-empty>
    </div>
  </div>
</template>

<style scoped>
.quiz-container {
  @apply h-screen flex flex-col bg-bg;
}

.dark .quiz-container {
  @apply bg-bg;
}

.loading-container,
.empty-container {
  @apply flex-1 flex flex-col items-center justify-center gap-5;
}

.loading-text {
  @apply text-base text-text-secondary;
}

/* 顶部导航栏 */
.quiz-header {
  @apply flex items-center justify-between px-6 py-3 bg-bg
         backdrop-blur-md border-b border-border shadow-xs;
}

.dark .quiz-header {
  @apply bg-bg border-b-border;
}

.header-left {
  @apply flex items-center gap-4;
}

.logo-section {
  @apply flex items-center gap-2;
}

.logo-icon {
  @apply text-primary;
}

.logo-title {
  @apply text-[1.125rem] font-semibold text-text-primary;
}

.dark .logo-title {
  @apply text-text-primary;
}

.quiz-info {
  @apply flex items-center gap-2 text-sm text-text-secondary;
}

.dark .quiz-info {
  @apply text-text-secondary;
}

.separator {
  @apply text-border;
}

.header-right {
  @apply flex items-center gap-3;
}

.progress-indicator,
.timer-indicator {
  @apply flex items-center gap-2 px-3 py-2 bg-bg-secondary rounded-md;
}

.dark .progress-indicator,
.dark .timer-indicator {
  @apply bg-bg-secondary;
}

.progress-text,
.timer-text {
  @apply text-sm font-medium text-text-primary;
}

.dark .progress-text,
.dark .timer-text {
  @apply text-text-primary;
}

.progress-indicator .el-progress {
  @apply w-20;
}

/* 主体内容 */
.quiz-body {
  @apply flex-1 flex overflow-hidden;
}

.quiz-main {
  @apply flex-1 flex flex-col overflow-hidden;
}

.answer-section {
  @apply flex-1 overflow-y-auto p-6;
}

/* 底部操作栏 */
.quiz-footer {
  @apply flex items-center justify-between px-4 py-3 bg-bg border-t border-border shadow-xs;
}

.dark .quiz-footer {
  @apply bg-bg border-t-border;
}

.footer-left,
.footer-right {
  @apply flex gap-3;
}

/* 滚动条样式 */
.answer-section::-webkit-scrollbar {
  width: 6px;
}

.answer-section::-webkit-scrollbar-thumb {
  background: var(--color-border);
  border-radius: 3px;
}

.answer-section::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-muted);
}
</style>
