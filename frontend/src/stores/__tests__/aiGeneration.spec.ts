import { beforeEach, describe, expect, it, vi } from 'vitest'
import { createPinia, setActivePinia } from 'pinia'
import { useAIGenerationStore } from '../aiGeneration'

const { mockGenerateQuestionsApi, mockGenerateQuestionsAsyncApi, mockGetGenerationProgressApi } =
  vi.hoisted(() => ({
    mockGenerateQuestionsApi: vi.fn(),
    mockGenerateQuestionsAsyncApi: vi.fn(),
    mockGetGenerationProgressApi: vi.fn()
  }))

vi.mock('@/api/aiGeneration', () => ({
  generateQuestionsApi: mockGenerateQuestionsApi,
  generateQuestionsAsyncApi: mockGenerateQuestionsAsyncApi,
  getGenerationProgressApi: mockGetGenerationProgressApi,
  normalizeGeneratedQuestion: (question: unknown) => question
}))

describe('aiGeneration store', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
  })

  it('处理中返回部分题目时应立即更新结果列表', async () => {
    const store = useAIGenerationStore()

    mockGetGenerationProgressApi.mockResolvedValue({
      taskId: 'task-1',
      userId: 1,
      status: 'processing',
      generatedCount: 1,
      totalCount: 10,
      createdAt: '2026-03-07T00:00:00.000Z',
      questions: [
        {
          id: 101,
          questionTypeEnum: 'SingleChoice',
          questionType: 'SingleChoice',
          questionText: 'Vue 3 中 ref 的访问方式是什么？',
          difficulty: 'medium',
          questionBankId: 5,
          createdAt: '2026-03-07T00:00:00.000Z',
          data: {
            type: 'choice',
            options: ['A. 直接访问', 'B. 使用 .value'],
            correctAnswers: ['B'],
            difficulty: 'medium'
          }
        }
      ]
    })

    await store.updateProgress('task-1')

    expect(store.taskStatus).toBe('processing')
    expect(store.generatedQuestions).toHaveLength(1)
    expect(store.generatedQuestions[0]?.questionText).toBe('Vue 3 中 ref 的访问方式是什么？')
  })
})
