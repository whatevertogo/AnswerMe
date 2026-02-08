import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface Question {
  id: string
  content: string
  type: 'choice' | 'multiple-choice' | 'true-false' | 'short-answer'
  options?: string[]
  correctAnswer: string
  explanation?: string
  difficulty: 'easy' | 'medium' | 'hard'
  tags: string[]
}

export interface QuestionBank {
  id: string
  name: string
  description: string
  questionCount: number
  createdAt: string
  updatedAt: string
}

export const useQuestionBankStore = defineStore('questionBank', () => {
  const questionBanks = ref<QuestionBank[]>([])
  const currentBank = ref<QuestionBank | null>(null)
  const questions = ref<Question[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchQuestionBanks() {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // const response = await api.get('/question-banks')
      // questionBanks.value = response.data
    } catch {
      error.value = 'Failed to fetch question banks'
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestionBank(id: string) {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // const response = await api.get(`/question-banks/${id}`)
      // currentBank.value = response.data
      void id // 占位符，避免未使用参数警告
    } catch {
      error.value = 'Failed to fetch question bank'
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestions(bankId: string) {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // const response = await api.get(`/question-banks/${bankId}/questions`)
      // questions.value = response.data
      void bankId // 占位符，避免未使用参数警告
    } catch {
      error.value = 'Failed to fetch questions'
    } finally {
      loading.value = false
    }
  }

  return {
    questionBanks,
    currentBank,
    questions,
    loading,
    error,
    fetchQuestionBanks,
    fetchQuestionBank,
    fetchQuestions
  }
})
