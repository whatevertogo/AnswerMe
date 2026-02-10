import { defineStore } from 'pinia'
import { ref } from 'vue'
import type {
  Question,
  QuestionQueryParams,
  CreateQuestionDto,
  UpdateQuestionDto
} from '@/types'
import * as questionApi from '@/api/question'
import { extractErrorMessage } from '@/utils/errorHandler'

export type { Question }

export const useQuestionStore = defineStore('question', () => {
  const questions = ref<Question[]>([])
  const currentQuestion = ref<Question | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const pagination = ref({
    totalCount: 0,
    pageSize: 20,
    hasMore: false,
    nextCursor: null as number | null
  })

  async function fetchQuestions(params: QuestionQueryParams, options?: { append?: boolean }) {
    loading.value = true
    error.value = null
    try {
      const queryParams: QuestionQueryParams = {
        pageSize: params.pageSize || 20,
        lastId: params.lastId,
        questionBankId: params.questionBankId,
        questionTypeEnum: params.questionTypeEnum,
        difficulty: params.difficulty,
        search: params.search
      }
      const response = await questionApi.getQuestions(queryParams)
      if (options?.append) {
        questions.value = [...questions.value, ...response.data.data]
      } else {
        questions.value = response.data.data
      }
      pagination.value = {
        totalCount: response.data.totalCount,
        pageSize: queryParams.pageSize,
        hasMore: response.data.hasMore,
        nextCursor: response.data.nextCursor ?? null
      }
    } catch (err) {
      error.value = extractErrorMessage(err, '获取题目列表失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestion(id: number) {
    loading.value = true
    error.value = null
    try {
      const response = await questionApi.getQuestionDetail(id)
      currentQuestion.value = response.data
      return response
    } catch (err) {
      error.value = extractErrorMessage(err, '获取题目详情失败')
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
      questions.value.unshift(result.data)
      pagination.value.total += 1
      return result.data
    } catch (err) {
      error.value = extractErrorMessage(err, '创建题目失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function updateQuestion(
    id: number,
    data: UpdateQuestionDto
  ): Promise<Question> {
    loading.value = true
    error.value = null
    try {
      const result = await questionApi.updateQuestion(id, data)
      // 更新列表中的数据
      const index = questions.value.findIndex(q => q.id === id)
      if (index !== -1) {
        questions.value[index] = result.data
      }
      // 更新当前题目数据
      if (currentQuestion.value?.id === id) {
        currentQuestion.value = result.data
      }
      return result.data
    } catch (err) {
      error.value = extractErrorMessage(err, '更新题目失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function deleteQuestion(id: number): Promise<void> {
    loading.value = true
    error.value = null
    try {
      await questionApi.deleteQuestion(id)
      questions.value = questions.value.filter(q => q.id !== id)
      pagination.value.total -= 1
      if (currentQuestion.value?.id === id) {
        currentQuestion.value = null
      }
    } catch (err) {
      error.value = extractErrorMessage(err, '删除题目失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function searchQuestions(searchTerm: string, questionBankId?: number): Promise<Question[]> {
    loading.value = true
    error.value = null
    try {
      const response = await questionApi.searchQuestions(searchTerm, questionBankId)
      questions.value = response.data
      return response.data
    } catch (err) {
      error.value = extractErrorMessage(err, '搜索题目失败')
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
      totalCount: 0,
      pageSize: 20,
      hasMore: false,
      nextCursor: null
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
