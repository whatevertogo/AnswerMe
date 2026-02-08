import { defineStore } from 'pinia'
import { ref } from 'vue'
import type {
  Question,
  QuestionBank,
  QuestionBankQueryParams,
  CreateQuestionBankDto,
  UpdateQuestionBankDto
} from '@/types'
import * as questionBankApi from '@/api/questionBank'

export type { QuestionBank, Question }

export const useQuestionBankStore = defineStore('questionBank', () => {
  const questionBanks = ref<QuestionBank[]>([])
  const currentBank = ref<QuestionBank | null>(null)
  const questions = ref<Question[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)
  const hasMore = ref(false)
  const nextCursor = ref<number | undefined>(undefined)

  async function fetchQuestionBanks(params?: QuestionBankQueryParams) {
    loading.value = true
    error.value = null
    try {
      const queryParams: QuestionBankQueryParams = {
        pageSize: params?.pageSize || 20,
        lastId: params?.lastId,
        search: params?.search
      }
      const response = await questionBankApi.getQuestionBanks(queryParams)
      if (params?.lastId) {
        questionBanks.value = [...questionBanks.value, ...response.data]
      } else {
        questionBanks.value = response.data
      }
      hasMore.value = response.hasMore
      nextCursor.value = response.nextCursor
    } catch {
      error.value = '获取题库列表失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestionBank(id: string) {
    loading.value = true
    error.value = null
    try {
      const response = await questionBankApi.getQuestionBankDetail(Number(id))
      currentBank.value = response
      questions.value = response.questions || []
      return response
    } catch {
      error.value = '获取题库详情失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function createQuestionBank(data: CreateQuestionBankDto): Promise<QuestionBank> {
    loading.value = true
    error.value = null
    try {
      const result = await questionBankApi.createQuestionBank(data)
      questionBanks.value.unshift(result)
      return result
    } catch {
      error.value = '创建题库失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function updateQuestionBank(
    id: string,
    data: UpdateQuestionBankDto
  ): Promise<QuestionBank> {
    loading.value = true
    error.value = null
    try {
      const result = await questionBankApi.updateQuestionBank(Number(id), data)
      // 更新列表中的数据
      const index = questionBanks.value.findIndex(b => b.id === id)
      if (index !== -1) {
        questionBanks.value[index] = result
      }
      // 更新当前题库数据
      if (currentBank.value?.id === id) {
        currentBank.value = result
      }
      return result
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : '更新题库失败'
      error.value = message
      throw message
    } finally {
      loading.value = false
    }
  }

  async function deleteQuestionBank(id: string): Promise<void> {
    loading.value = true
    error.value = null
    try {
      await questionBankApi.deleteQuestionBank(Number(id))
      questionBanks.value = questionBanks.value.filter(b => b.id !== id)
      if (currentBank.value?.id === id) {
        currentBank.value = null
        questions.value = []
      }
    } catch {
      error.value = '删除题库失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function searchQuestionBanks(searchTerm: string): Promise<QuestionBank[]> {
    loading.value = true
    error.value = null
    try {
      const results = await questionBankApi.searchQuestionBanks(searchTerm)
      questionBanks.value = results
      hasMore.value = false
      return results
    } catch {
      error.value = '搜索题库失败'
      throw error.value
    } finally {
      loading.value = false
    }
  }

  function clearCurrentBank() {
    currentBank.value = null
    questions.value = []
  }

  return {
    questionBanks,
    currentBank,
    questions,
    loading,
    error,
    hasMore,
    nextCursor,
    fetchQuestionBanks,
    fetchQuestionBank,
    createQuestionBank,
    updateQuestionBank,
    deleteQuestionBank,
    searchQuestionBanks,
    clearCurrentBank
  }
})
