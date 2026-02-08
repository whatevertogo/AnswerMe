<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { Clock, CheckCircle, Flag, ArrowLeft, ArrowRight, Moon, Sunny, Home, Reading } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import QuizQuestionList from '@/components/quiz/QuizQuestionList.vue'
import QuizQuestionPanel from '@/components/quiz/QuizQuestionPanel.vue'
import QuizAnswerPanel from '@/components/quiz/QuizAnswerPanel.vue'
import QuizResultModal from '@/components/quiz/QuizResultModal.vue'

const router = useRouter()

interface Question {
  id: number
  content: string
  type: 'single' | 'multiple' | 'essay'
  options: string[]
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string[]
  explanation?: string
}

// Mock data - 实际应该从 API 获取
const mockQuestions: Question[] = [
  {
    id: 1,
    content: '以下哪个是 JavaScript 中声明常量的关键字?',
    type: 'single',
    options: ['var', 'let', 'const', 'static'],
    difficulty: 'easy',
    tags: ['JavaScript', '基础'],
    explanation: 'const 用于声明一个只读的常量,一旦声明,常量的值就不能改变。'
  },
  {
    id: 2,
    content: 'React 中哪个生命周期方法会在组件渲染后立即调用?(多选)',
    type: 'multiple',
    options: ['componentDidMount', 'useEffect', 'componentDidUpdate', 'componentWillUnmount'],
    difficulty: 'medium',
    tags: ['React', '生命周期'],
    explanation: 'componentDidMount 和 useEffect (空依赖数组) 都会在组件首次渲染后立即执行。'
  },
  {
    id: 3,
    content: '解释 Vue 3 Composition API 的优势和应用场景。',
    type: 'essay',
    options: [],
    difficulty: 'hard',
    tags: ['Vue', '架构设计'],
    explanation: 'Composition API 提供了更好的逻辑复用、类型推断和代码组织方式。'
  },
  {
    id: 4,
    content: 'CSS 中 display: flex 和 display: grid 的主要区别是什么?',
    type: 'single',
    options: [
      'flex 用于一维布局,grid 用于二维布局',
      'grid 用于一维布局,flex 用于二维布局',
      '两者完全相同',
      'flex 只能水平排列,grid 只能垂直排列'
    ],
    difficulty: 'medium',
    tags: ['CSS', '布局'],
    explanation: 'Flexbox 是一维布局系统(行或列),Grid 是二维布局系统(行和列同时控制)。'
  },
  {
    id: 5,
    content: '以下哪些是 TypeScript 相比 JavaScript 的优势?(多选)',
    type: 'multiple',
    options: ['静态类型检查', '更好的 IDE 支持', '编译时错误发现', '运行时性能提升'],
    difficulty: 'easy',
    tags: ['TypeScript', '理论'],
    explanation: 'TypeScript 提供静态类型、更好的 IDE 智能提示和编译时错误检测,但不会提升运行时性能。'
  }
]

// State
const currentQuestionIndex = ref(0)
const answers = ref<Record<number, string | string[]>>({})
const markedQuestions = ref<Set<number>>(new Set())
const timeElapsed = ref(0)
const showResult = ref(false)
const isSubmitted = ref(false)
const darkMode = ref(true)

// Computed
const currentQuestion = computed(() => mockQuestions[currentQuestionIndex.value])
const answeredCount = computed(() => Object.keys(answers.value).length)
const progress = computed(() => (answeredCount.value / mockQuestions.length) * 100)

// Timer
let timer: number | null = null
onMounted(() => {
  timer = window.setInterval(() => {
    timeElapsed.value++
  }, 1000)
})

// Methods
const formatTime = (seconds: number) => {
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
}

const handleAnswer = (questionId: number, answer: string | string[]) => {
  answers.value[questionId] = answer
}

const toggleMark = (questionId: number) => {
  if (markedQuestions.value.has(questionId)) {
    markedQuestions.value.delete(questionId)
  } else {
    markedQuestions.value.add(questionId)
  }
}

const goToQuestion = (index: number) => {
  currentQuestionIndex.value = index
}

const goToNext = () => {
  if (currentQuestionIndex.value < mockQuestions.length - 1) {
    currentQuestionIndex.value++
  }
}

const goToPrevious = () => {
  if (currentQuestionIndex.value > 0) {
    currentQuestionIndex.value--
  }
}

const handleSubmit = () => {
  isSubmitted.value = true
  showResult.value = true
}

const goHome = () => {
  router.push('/home')
}

const toggleDarkMode = () => {
  darkMode.value = !darkMode.value
  // TODO: 实现深色模式切换逻辑
}

// Cleanup
onMounted(() => {
  return () => {
    if (timer) clearInterval(timer)
  }
})
</script>

<template>
  <div class="quiz-container">
    <!-- 顶部导航栏 -->
    <header class="quiz-header">
      <div class="header-left">
        <div class="logo-section">
          <el-icon :size="20" class="logo-icon"><Reading /></el-icon>
          <h1 class="logo-title">AnswerMe</h1>
        </div>
        <el-divider direction="vertical" />
        <div class="quiz-info">
          <span>前端开发测试</span>
          <span class="separator">·</span>
          <span>{{ mockQuestions.length }} 题</span>
        </div>
      </div>

      <div class="header-right">
        <!-- 进度 -->
        <div class="progress-indicator">
          <el-icon :size="16" color="#10b981"><CheckCircle /></el-icon>
          <span class="progress-text">{{ answeredCount }}/{{ mockQuestions.length }}</span>
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
          <el-icon><Home /></el-icon>
          返回首页
        </el-button>
      </div>
    </header>

    <!-- 主体内容 -->
    <div class="quiz-body">
      <!-- 左侧题目列表 -->
      <QuizQuestionList
        :questions="mockQuestions"
        :current-index="currentQuestionIndex"
        :answers="answers"
        :marked-questions="markedQuestions"
        @question-click="goToQuestion"
      />

      <!-- 中间题目面板 -->
      <div class="quiz-main">
        <QuizQuestionPanel
          :question="currentQuestion"
          :question-number="currentQuestionIndex + 1"
          :total-questions="mockQuestions.length"
        />

        <!-- 答题区域 -->
        <div class="answer-section">
          <QuizAnswerPanel
            :question="currentQuestion"
            :answer="answers[currentQuestion.id]"
            :disabled="isSubmitted"
            @update:answer="(ans) => handleAnswer(currentQuestion.id, ans)"
          />
        </div>
      </div>
    </div>

    <!-- 底部操作栏 -->
    <footer class="quiz-footer">
      <div class="footer-left">
        <el-button
          :type="markedQuestions.has(currentQuestion.id) ? 'warning' : 'default'"
          :icon="Flag"
          @click="toggleMark(currentQuestion.id)"
        >
          {{ markedQuestions.has(currentQuestion.id) ? '已标记' : '标记' }}
        </el-button>
      </div>

      <div class="footer-right">
        <el-button
          :icon="ArrowLeft"
          :disabled="currentQuestionIndex === 0"
          @click="goToPrevious"
        >
          上一题
        </el-button>

        <el-button
          :icon="ArrowRight"
          :disabled="currentQuestionIndex === mockQuestions.length - 1"
          @click="goToNext"
        >
          下一题
        </el-button>

        <el-button
          type="primary"
          :disabled="answeredCount === 0"
          @click="handleSubmit"
        >
          交卷
        </el-button>
      </div>
    </footer>

    <!-- 结果弹窗 -->
    <QuizResultModal
      v-model:visible="showResult"
      :questions="mockQuestions"
      :answers="answers"
      :time-elapsed="timeElapsed"
    />
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
