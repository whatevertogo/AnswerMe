import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import * as wrongQuestionApi from '@/api/wrongQuestion'
import type {
  WrongQuestion,
  WrongQuestionListResponse,
  LearningStatsResponse,
  WrongQuestionQueryParams
} from '@/api/wrongQuestion'

export const useWrongQuestionStore = defineStore('wrongQuestion', () => {
  // State
  const questions = ref<WrongQuestion[]>([])
  const totalCount = ref(0)
  const bankGroupCount = ref(0)
  const stats = ref<LearningStatsResponse | null>(null)
  const loading = ref(false)
  const error = ref<string | null>(null)
  const filters = ref<WrongQuestionQueryParams>({
    questionBankId: undefined,
    questionType: undefined,
    startDate: undefined,
    endDate: undefined,
    pageIndex: 1,
    pageSize: 20
  })

  // Computed
  const questionsByBank = computed(() => {
    const grouped: Record<number, WrongQuestion[]> = {}
    for (const q of questions.value) {
      const bankId = q.questionBankId
      if (!grouped[bankId]) {
        grouped[bankId] = []
      }
      grouped[bankId].push(q)
    }
    return grouped
  })

  // Actions
  async function fetchWrongQuestions(params?: WrongQuestionQueryParams) {
    loading.value = true
    error.value = null
    try {
      const mergedFilters = { ...filters.value, ...params }
      const response: WrongQuestionListResponse =
        await wrongQuestionApi.getWrongQuestions(mergedFilters)
      questions.value = response.questions
      totalCount.value = response.totalCount
      bankGroupCount.value = response.bankGroupCount
      return response
    } catch (e) {
      error.value = e instanceof Error ? e.message : '获取错题列表失败'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchStats() {
    loading.value = true
    error.value = null
    try {
      stats.value = await wrongQuestionApi.getLearningStats()
      return stats.value
    } catch (e) {
      error.value = e instanceof Error ? e.message : '获取学习统计失败'
      throw e
    } finally {
      loading.value = false
    }
  }

  async function markAsMastered(detailId: number) {
    loading.value = true
    error.value = null
    try {
      await wrongQuestionApi.markAsMastered(detailId)
      // 从列表中移除
      const index = questions.value.findIndex(q => q.id === detailId)
      if (index !== -1) {
        const removedBankId = questions.value[index]?.questionBankId
        questions.value.splice(index, 1)
        totalCount.value = Math.max(0, totalCount.value - 1)
        if (
          removedBankId !== undefined &&
          !questions.value.some(q => q.questionBankId === removedBankId)
        ) {
          bankGroupCount.value = Math.max(0, bankGroupCount.value - 1)
        }
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : '标记失败'
      throw e
    } finally {
      loading.value = false
    }
  }

  function setFilters(newFilters: Partial<WrongQuestionQueryParams>) {
    filters.value = { ...filters.value, ...newFilters }
  }

  function resetFilters() {
    filters.value = {
      questionBankId: undefined,
      questionType: undefined,
      startDate: undefined,
      endDate: undefined,
      pageIndex: 1,
      pageSize: 20
    }
  }

  return {
    // State
    questions,
    stats,
    loading,
    error,
    filters,
    // Computed
    totalCount,
    bankGroupCount,
    questionsByBank,
    // Actions
    fetchWrongQuestions,
    fetchStats,
    markAsMastered,
    setFilters,
    resetFilters
  }
})
