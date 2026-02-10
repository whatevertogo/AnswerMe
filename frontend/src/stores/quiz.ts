import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as quizApi from '@/api/quiz'

export interface QuizQuestion {
  id: number
  content: string
  type: 'single' | 'multiple' | 'boolean' | 'fill' | 'essay'
  options: string[]
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string[]
  correctAnswer?: string
  explanation?: string
}

export interface QuizAnswer {
  questionId: number
  userAnswer: string
  timeSpent?: number
}

export interface QuizResult {
  id: number
  userId: number
  questionBankId: number
  score: number
  totalQuestions: number
  correctCount: number
  completedAt: string
  details: QuizDetail[]
}

export interface QuizDetail {
  questionId: number
  userAnswer: string | string[]
  correctAnswer: string | string[]
  isCorrect: boolean
  timeSpent: number
}

export interface QuizStatistics {
  totalAttempts: number
  completedAttempts: number
  averageScore: number | null
  totalQuestionsAnswered: number
  totalCorrectAnswers: number
  overallAccuracy: number | null
}

/**
 * 比较多选题答案（全对才得分）
 * @param userAnswer 用户答案（字符串或数组）
 * @param correctAnswer 正确答案（字符串或数组）
 * @returns 是否正确
 */
export function compareMultipleChoiceAnswers(
  userAnswer: string | string[],
  correctAnswer: string | string[]
): boolean {
  // 将答案统一转换为数组
  const userArray = typeof userAnswer === 'string'
    ? userAnswer.split(',').map(a => a.trim())
    : userAnswer

  const correctArray = typeof correctAnswer === 'string'
    ? correctAnswer.split(',').map(a => a.trim())
    : correctAnswer

  // 数量必须相同
  if (userArray.length !== correctArray.length) {
    return false
  }

  // 排序后比较
  const sortedUser = [...userArray].sort()
  const sortedCorrect = [...correctArray].sort()

  // 每个答案都必须匹配
  return sortedUser.every((answer, index) => answer === sortedCorrect[index])
}

/**
 * 判断题目是否为多选题
 */
export function isMultipleChoiceQuestion(type: string): boolean {
  return type === 'multiple' || type === 'MultipleChoice' || type === 'multiple-choice'
}

export const useQuizStore = defineStore('quiz', () => {
  // State
  const currentAttemptId = ref<number | null>(null)
  const questionIds = ref<number[]>([])
  const currentQuestionIndex = ref(0)
  // 支持字符串或字符串数组（多选题）
  const answers = ref<Record<number, string | string[]>>({})
  const timeSpents = ref<Record<number, number>>({})
  const startedAt = ref<Date | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const result = ref<QuizResult | null>(null)
  const details = ref<QuizDetail[]>([])

  // Computed
  const currentQuestionId = computed(() => {
    if (currentQuestionIndex.value < questionIds.value.length) {
      return questionIds.value[currentQuestionIndex.value]
    }
    return null
  })

  const isLastQuestion = computed(() => {
    return currentQuestionIndex.value === questionIds.value.length - 1
  })

  const answeredCount = computed(() => {
    return Object.keys(answers.value).length
  })

  const progress = computed(() => {
    if (questionIds.value.length === 0) return 0
    return (answeredCount.value / questionIds.value.length) * 100
  })

  // Actions
  async function startQuiz(questionBankId: number, mode: 'sequential' | 'random' = 'sequential') {
    loading.value = true
    error.value = null
    try {
      const response = await quizApi.startQuiz({ questionBankId, mode })
      currentAttemptId.value = response.data.attemptId
      questionIds.value = response.data.questionIds || []
      currentQuestionIndex.value = 0
      answers.value = {}
      timeSpents.value = {}
      startedAt.value = new Date()
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || '开始答题失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function submitAnswer(questionId: number, userAnswer: string | string[], timeSpent?: number) {
    if (!currentAttemptId.value) {
      throw new Error('答题未开始')
    }

    loading.value = true
    error.value = null
    try {
      // 将数组答案转换为逗号分隔字符串（后端期望格式：A,B,C）
      const answerString = Array.isArray(userAnswer)
        ? userAnswer.join(',')
        : userAnswer

      await quizApi.submitAnswer({
        attemptId: currentAttemptId.value,
        questionId,
        userAnswer: answerString,
        timeSpent
      })

      // 本地保存答案（保持原格式）
      answers.value[questionId] = userAnswer
      if (timeSpent) {
        timeSpents.value[questionId] = timeSpent
      }

      return true
    } catch (err: any) {
      error.value = err.response?.data?.message || '提交答案失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function completeQuiz() {
    if (!currentAttemptId.value) {
      throw new Error('答题未开始')
    }

    loading.value = true
    error.value = null
    try {
      const response = await quizApi.completeQuiz({
        attemptId: currentAttemptId.value
      })
      result.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || '完成答题失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuizResult(attemptId: number) {
    loading.value = true
    error.value = null
    try {
      const response = await quizApi.getQuizResult(attemptId)
      result.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || '获取答题结果失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuizDetails(attemptId: number) {
    loading.value = true
    error.value = null
    try {
      const response = await quizApi.getQuizDetails(attemptId)
      details.value = response.data
      return response.data
    } catch (err: any) {
      error.value = err.response?.data?.message || '获取答题详情失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuizStatistics() {
    loading.value = true
    error.value = null
    try {
      const response = await quizApi.getQuizStatistics()
      return response
    } catch (err: any) {
      error.value = err.response?.data?.message || '获取答题统计失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  function goToQuestion(index: number) {
    currentQuestionIndex.value = index
  }

  function goToNext() {
    if (currentQuestionIndex.value < questionIds.value.length - 1) {
      currentQuestionIndex.value++
    }
  }

  function goToPrevious() {
    if (currentQuestionIndex.value > 0) {
      currentQuestionIndex.value--
    }
  }

  function reset() {
    currentAttemptId.value = null
    questionIds.value = []
    currentQuestionIndex.value = 0
    answers.value = {}
    timeSpents.value = {}
    startedAt.value = null
    result.value = null
    details.value = []
    error.value = null
  }

  return {
    // State
    currentAttemptId,
    questionIds,
    currentQuestionIndex,
    currentQuestionId,
    answers,
    timeSpents,
    startedAt,
    loading,
    error,
    result,
    details,

    // Computed
    isLastQuestion,
    answeredCount,
    progress,

    // Actions
    startQuiz,
    submitAnswer,
    completeQuiz,
    fetchQuizResult,
    fetchQuizDetails,
    fetchQuizStatistics,
    goToQuestion,
    goToNext,
    goToPrevious,
    reset
  }
})
