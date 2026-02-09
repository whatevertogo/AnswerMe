import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import {
  generateQuestionsApi,
  generateQuestionsAsyncApi,
  getGenerationProgressApi,
  type AIGenerateRequest,
  type AIGenerateResponse,
  type AIGenerateProgress,
  type GeneratedQuestion,
  type TaskStatus
} from '@/api/aiGeneration'

export const useAIGenerationStore = defineStore('aiGeneration', () => {
  // State
  const loading = ref(false)
  const generating = ref(false)
  const currentRequest = ref<AIGenerateRequest | null>(null)
  const currentResponse = ref<AIGenerateResponse | null>(null)
  const currentTaskId = ref<string | null>(null)
  const progress = ref<AIGenerateProgress | null>(null)
  const pollTimer = ref<number | null>(null)
  const error = ref<string | null>(null)
  const generatedQuestions = ref<GeneratedQuestion[]>([])

  // Computed
  const isAsyncTask = computed(() => !!currentTaskId.value)
  const progressPercentage = computed(() => {
    if (!progress.value) return 0
    const { generatedCount, totalCount } = progress.value
    return totalCount > 0 ? Math.round((generatedCount / totalCount) * 100) : 0
  })
  const taskStatus = computed<TaskStatus>(() => progress.value?.status || 'pending')
  const isCompleted = computed(() => {
    const status = taskStatus.value
    return status === 'completed' || status === 'failed' || status === 'partial_success'
  })

  // Actions
  async function generateQuestions(params: AIGenerateRequest) {
    loading.value = true
    error.value = null
    currentRequest.value = params
    currentResponse.value = null
    currentTaskId.value = null
    progress.value = null
    generatedQuestions.value = []

    try {
      // 根据题目数量选择同步或异步
      const useAsync = params.count > 20

      if (useAsync) {
        // 异步生成
        const response = await generateQuestionsAsyncApi(params)
        currentResponse.value = response

        if (response.success && response.taskId) {
          currentTaskId.value = response.taskId
          await startProgressPolling(response.taskId)
        } else {
          error.value = response.errorMessage || '创建异步任务失败'
        }
      } else {
        // 同步生成
        const response = await generateQuestionsApi(params)
        currentResponse.value = response

        if (response.success) {
          generatedQuestions.value = response.questions
        } else {
          error.value = response.errorMessage || '生成失败'
        }
      }
    } catch (err: any) {
      error.value = err.response?.data?.message || err.message || '生成题目时发生错误'
      console.error('生成题目失败:', err)
    } finally {
      loading.value = false
    }
  }

  async function startProgressPolling(taskId: string) {
    currentTaskId.value = taskId
    generating.value = true

    // 立即查询一次
    await updateProgress(taskId)

    // 每2秒轮询一次
    pollTimer.value = window.setInterval(() => {
      updateProgress(taskId)
    }, 2000)
  }

  async function updateProgress(taskId: string) {
    try {
      const response = await getGenerationProgressApi(taskId)
      progress.value = response

      // 如果任务完成，停止轮询
      if (isCompleted.value) {
        stopProgressPolling()

        // 如果有生成的题目，保存到结果中
        if (response.questions && response.questions.length > 0) {
          generatedQuestions.value = response.questions
        }

        // 如果失败，设置错误信息
        if (response.status === 'failed' && response.errorMessage) {
          error.value = response.errorMessage
        }
      }
    } catch (err: any) {
      console.error('查询进度失败:', err)
      // 不中断轮询，可能是网络临时问题
    }
  }

  function stopProgressPolling() {
    if (pollTimer.value) {
      clearInterval(pollTimer.value)
      pollTimer.value = null
    }
    generating.value = false
  }

  function reset() {
    stopProgressPolling()
    loading.value = false
    generating.value = false
    currentRequest.value = null
    currentResponse.value = null
    currentTaskId.value = null
    progress.value = null
    error.value = null
    generatedQuestions.value = []
  }

  function cancelGeneration() {
    stopProgressPolling()
    generating.value = false
  }

  return {
    // State
    loading,
    generating,
    currentRequest,
    currentResponse,
    currentTaskId,
    progress,
    error,
    generatedQuestions,

    // Computed
    isAsyncTask,
    progressPercentage,
    taskStatus,
    isCompleted,

    // Actions
    generateQuestions,
    startProgressPolling,
    updateProgress,
    stopProgressPolling,
    reset,
    cancelGeneration
  }
})
