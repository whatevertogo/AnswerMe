import { defineStore } from 'pinia'
import { ref } from 'vue'
import type {
  Question,
  QuestionQueryParams,
  CreateQuestionDto,
  UpdateQuestionDto
} from '@/types'
import * as questionApi from '@/api/question'
import { QuestionType, Difficulty } from '@/types'

export type { Question }
export { QuestionType, Difficulty }

export const useQuestionStore = defineStore('question', () => {
  const questions = ref<Question[]>([])
  const currentQuestion = ref<Question | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const pagination = ref({
    total: 0,
    page: 1,
    pageSize: 20
  })

  async function fetchQuestions(params: QuestionQueryParams) {
    loading.value = true
    error.value = null
    try {
      const queryParams: QuestionQueryParams = {
        page: params.page || 1,
        pageSize: params.pageSize || 20,
        questionBankId: params.questionBankId,
        type: params.type,
        difficulty: params.difficulty,
        search: params.search
      }
      const response = await questionApi.getQuestions(queryParams)
      questions.value = response
      pagination.value = {
        total: response.total,
        page: response.page,
        pageSize: response.pageSize
      }
    } catch {
      error.value = '获取题目列表失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestion(id: string) {
    loading.value = true
    error.value = null
    try {
      const response = await questionApi.getQuestionDetail(id)
      currentQuestion.value = response
      return response
    } catch {
      error.value = '获取题目详情失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function createQuestion(data: CreateQuestionDto): Promise<Question> {
    loading.value = true
    error.value = null
    try {
      const result = await questionApi.createQuestion(data)
      questions.value.unshift(result)
      pagination.value.total += 1
      return result
    } catch {
      error.value = '创建题目失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function updateQuestion(
    id: string,
    data: UpdateQuestionDto
  ): Promise<Question> {
    loading.value = true
    error.value = null
    try {
      const result = await questionApi.updateQuestion(id, data)
      // 更新列表中的数据
      const index = questions.value.findIndex(q => q.id === id)
      if (index !== -1) {
        questions.value[index] = result
      }
      // 更新当前题目数据
      if (currentQuestion.value?.id === id) {
        currentQuestion.value = result
      }
      return result
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : '更新题目失败'
      error.value = message
      throw message
    } finally {
      loading.value = false
    }
  }

  async function deleteQuestion(id: string): Promise<void> {
    loading.value = true
    error.value = null
    try {
      await questionApi.deleteQuestion(id)
      questions.value = questions.value.filter(q => q.id !== id)
      pagination.value.total -= 1
      if (currentQuestion.value?.id === id) {
        currentQuestion.value = null
      }
    } catch {
      error.value = '删除题目失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function searchQuestions(searchTerm: string, questionBankId?: string): Promise<Question[]> {
    loading.value = true
    error.value = null
    try {
      const results = await questionApi.searchQuestions(searchTerm, questionBankId)
      questions.value = results
      return results
    } catch {
      error.value = '搜索题目失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  function clearCurrentQuestion() {
    currentQuestion.value = null
  }

  function clearQuestions() {
    questions.value = []
    currentQuestion.value = null
    pagination.value = {
      total: 0,
      page: 1,
      pageSize: 20
    }
  }

  return {
    questions,
    currentQuestion,
    loading,
    error,
    pagination,
    fetchQuestions,
    fetchQuestion,
    createQuestion,
    updateQuestion,
    deleteQuestion,
    searchQuestions,
    clearCurrentQuestion,
    clearQuestions
  }
})
