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
import * as questionApi from '@/api/question'
import { extractErrorMessage } from '@/utils/errorHandler'

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
    } catch (err) {
      error.value = extractErrorMessage(err, '获取题库列表失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function fetchQuestionBank(id: number) {
    loading.value = true
    error.value = null
    try {
      const response = await questionBankApi.getQuestionBankDetail(id)
      currentBank.value = response

      try {
        const questionsResponse = await questionApi.getQuestions({
          questionBankId: id,
          pageSize: 1000
        })
        // questionsResponse 的结构是 { data: [], hasMore, nextCursor, totalCount }
        questions.value = questionsResponse.data ?? []
      } catch (questionsError) {
        console.error('获取题库题目列表失败:', questionsError)
        questions.value = []
      }

      return response
    } catch (err) {
      error.value = extractErrorMessage(err, '获取题库详情失败')
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
    } catch (err) {
      error.value = extractErrorMessage(err, '创建题库失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function updateQuestionBank(
    id: number,
    data: UpdateQuestionBankDto
  ): Promise<QuestionBank> {
    loading.value = true
    error.value = null
    try {
      const result = await questionBankApi.updateQuestionBank(id, data)
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
    } catch (err) {
      error.value = extractErrorMessage(err, '更新题库失败')
      throw error.value
    } finally {
      loading.value = false
    }
  }

  async function deleteQuestionBank(id: number): Promise<void> {
    loading.value = true
    error.value = null
    try {
      await questionBankApi.deleteQuestionBank(id)
      questionBanks.value = questionBanks.value.filter(b => b.id !== id)
      if (currentBank.value?.id === id) {
        currentBank.value = null
        questions.value = []
      }
    } catch (err) {
      error.value = extractErrorMessage(err, '删除题库失败')
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
    } catch (err) {
      error.value = extractErrorMessage(err, '搜索题库失败')
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
