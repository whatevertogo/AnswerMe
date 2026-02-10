<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { Clock, CircleCheck, Flag, ArrowLeft, ArrowRight, Sunny, Moon, House, Reading } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import QuizQuestionList from '@/components/quiz/QuizQuestionList.vue'
import QuizQuestionPanel from '@/components/quiz/QuizQuestionPanel.vue'
import QuizAnswerPanel from '@/components/quiz/QuizAnswerPanel.vue'
import QuizResultModal from '@/components/quiz/QuizResultModal.vue'
import { useQuizStore } from '@/stores/quiz'
import { getQuestionDetail } from '@/api/question'
import type { QuizQuestion } from '@/stores/quiz'

const router = useRouter()
const route = useRoute()
const quizStore = useQuizStore()

// 从路由参数获取 bankId 和 attemptId (sessionId)
const bankId = computed(() => parseInt(route.params.bankId as string))
const sessionId = computed(() => route.params.sessionId as string)

// 本地状态
const questions = ref<QuizQuestion[]>([])
const questionMap = ref<Map<number, QuizQuestion>>(new Map())
const markedQuestions = ref<Set<number>>(new Set())
const timeElapsed = ref(0)
const showResult = ref(false)
const darkMode = ref(true)
const loading = ref(true)
const initializing = ref(true)

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
watch(() => route.params, async () => {
  if (route.name === 'Quiz') {
    stopTimer()
    await initializeQuiz()
    startTimer()
  }
})

async function initializeQuiz() {
  initializing.value = true
  loading.value = true

  try {
    // 如果 sessionId 是 'new' 或不存在，则开始新答题
    if (!sessionId.value || sessionId.value === 'new' || sessionId.value === 'undefined') {
      // 开始新答题
      await startNewQuiz()
    } else {
      // 加载已有答题
      await loadExistingQuiz()
    }
  } catch (error: any) {
    ElMessage.error('初始化答题失败: ' + (error.message || '未知错误'))
    console.error('初始化答题失败:', error)
    router.back()
  } finally {
    initializing.value = false
    loading.value = false
  }
}

async function startNewQuiz() {
  try {
    // 调用 startQuiz API
    await quizStore.startQuiz(bankId.value, 'sequential')

    // 获取题目详情
    await loadQuestionsDetails(quizStore.questionIds)

    ElMessage.success('答题开始！')
  } catch (error: any) {
    throw error
  }
}

async function loadExistingQuiz() {
  try {
    const attemptId = parseInt(sessionId.value)

    // 加载答题详情
    const details = await quizStore.fetchQuizDetails(attemptId)

    // 从详情中提取题目ID
    const questionIds = details.map((d: any) => d.questionId)

    // 更新 store 状态
    quizStore.currentAttemptId = attemptId
    quizStore.questionIds = questionIds

    // 恢复答案
    details.forEach((detail: any) => {
      if (detail.userAnswer) {
        quizStore.answers[detail.questionId] = detail.userAnswer
      }
    })

    // 获取题目详情
    await loadQuestionsDetails(questionIds)

    // 检查是否已完成
    const completedDetail = details.find((d: any) => d.completedAt !== null)
    if (completedDetail) {
      // 已完成，直接显示结果
      showResult.value = true
    }
  } catch (error: any) {
    throw error
  }
}

async function loadQuestionsDetails(questionIds: number[]) {
  try {
    // 批量获取题目详情
    const questionPromises = questionIds.map(id => getQuestionDetail(id))
    const questionDetails = await Promise.all(questionPromises)

    // 转换为 QuizQuestion 格式
    questions.value = questionDetails.map((q: any) => ({
      id: q.id,
      content: q.questionText,
      type: convertQuestionType(q.questionType),
      options: parseOptions(q.options),
      difficulty: q.difficulty || 'medium',
      tags: [],
      correctAnswer: q.correctAnswer,
      explanation: q.explanation
    }))

    // 创建映射
    questionMap.value = new Map(questions.value.map(q => [q.id, q]))
  } catch (error: any) {
    ElMessage.error('加载题目详情失败: ' + (error.message || '未知错误'))
    throw error
  }
}

function convertQuestionType(type: string): QuizQuestion['type'] {
  const typeMap: Record<string, QuizQuestion['type']> = {
    'choice': 'single',
    'multiple-choice': 'multiple',
    'true-false': 'boolean',
    'short-answer': 'essay'
  }
  return typeMap[type] || 'single'
}

function parseOptions(optionsStr?: string): string[] {
  if (!optionsStr) return []
  try {
    return JSON.parse(optionsStr)
  } catch {
    return []
  }
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
  try {
    let answerStr: string
    if (Array.isArray(answer)) {
      answerStr = JSON.stringify(answer)
    } else {
      answerStr = answer
    }

    await quizStore.submitAnswer(questionId, answerStr, timeElapsed.value)
  } catch (error: any) {
    ElMessage.error('提交答案失败: ' + (error.message || '未知错误'))
    console.error('提交答案失败:', error)
  }
}

const toggleMark = (questionId: number) => {
  if (markedQuestions.value.has(questionId)) {
    markedQuestions.value.delete(questionId)
  } else {
    markedQuestions.value.add(questionId)
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
    await quizStore.completeQuiz()
    showResult.value = true
    ElMessage.success('交卷成功！')
  } catch (error: any) {
    ElMessage.error('交卷失败: ' + (error.message || '未知错误'))
    console.error('交卷失败:', error)
  } finally {
    loading.value = false
  }
}

const goHome = () => {
  router.push('/home')
}

const toggleDarkMode = () => {
  darkMode.value = !darkMode.value
  // TODO: 实现深色模式切换逻辑
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
            <el-icon :size="16" color="#10b981"><CircleCheck /></el-icon>
            <span class="progress-text">{{ answeredCount }}/{{ totalQuestions }}</span>
            <el-progress :percentage="progress" :show-text="false" :stroke-width="6" />
          </div>

          <!-- 计时器 -->
          <div class="timer-indicator">
            <el-icon :size="16" color="#3b82f6"><Clock /></el-icon>
            <span class="timer-text">{{ formatTime(timeElapsed) }}</span>
          </div>

          <!-- 深色模式切换 -->
          <el-button :icon="darkMode ? Sunny : Moon" circle size="small" @click="toggleDarkMode" />

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
              :disabled="quizStore.loading || showResult"
              @update:answer="(ans) => handleAnswer(safeCurrentQuestion.id, ans)"
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
            :disabled="answeredCount === 0 || showResult"
            :loading="loading"
            @click="handleSubmit"
          >
            交卷
          </el-button>
        </div>
      </footer>

      <!-- 结果弹窗 -->
      <QuizResultModal
        v-model:visible="showResult"
        :questions="questions"
        :answers="quizStore.answers"
        :time-elapsed="timeElapsed"
        :attempt-id="quizStore.currentAttemptId"
      />
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
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: linear-gradient(to bottom right, #f8fafc, #f1f5f9);
}

.dark .quiz-container {
  background: linear-gradient(to bottom right, #030712, #111827);
}

.loading-container,
.empty-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 20px;
}

.loading-text {
  font-size: 16px;
  color: #606266;
}

/* 顶部导航栏 */
.quiz-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.75rem 1.5rem;
  background: rgba(255, 255, 255, 0.8);
  backdrop-filter: blur(8px);
  border-bottom: 1px solid #e5e7eb;
}

.dark .quiz-header {
  background: rgba(17, 24, 39, 0.8);
  border-bottom-color: #374151;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.logo-section {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.logo-icon {
  color: #3b82f6;
}

.logo-title {
  font-size: 1.125rem;
  font-weight: 600;
  color: #111827;
}

.dark .logo-title {
  color: #f9fafb;
}

.quiz-info {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.875rem;
  color: #6b7280;
}

.dark .quiz-info {
  color: #9ca3af;
}

.separator {
  color: #d1d5db;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.progress-indicator,
.timer-indicator {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  background: #f3f4f6;
  border-radius: 0.5rem;
}

.dark .progress-indicator,
.dark .timer-indicator {
  background: #1f2937;
}

.progress-text,
.timer-text {
  font-size: 0.875rem;
  font-weight: 500;
  color: #374151;
}

.dark .progress-text,
.dark .timer-text {
  color: #d1d5db;
}

.progress-indicator .el-progress {
  width: 5rem;
}

/* 主体内容 */
.quiz-body {
  flex: 1;
  display: flex;
  overflow: hidden;
}

.quiz-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.answer-section {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
}

/* 底部操作栏 */
.quiz-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.625rem 1rem;
  background: #ffffff;
  border-top: 1px solid #e5e7eb;
}

.dark .quiz-footer {
  background: #111827;
  border-top-color: #374151;
}

.footer-left,
.footer-right {
  display: flex;
  gap: 0.75rem;
}

/* 滚动条样式 */
.answer-section::-webkit-scrollbar {
  width: 6px;
}

.answer-section::-webkit-scrollbar-thumb {
  background: rgba(156, 163, 175, 0.5);
  border-radius: 3px;
}

.answer-section::-webkit-scrollbar-thumb:hover {
  background: rgba(156, 163, 175, 0.7);
}
</style>
