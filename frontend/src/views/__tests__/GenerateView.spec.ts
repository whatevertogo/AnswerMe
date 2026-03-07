import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import ElementPlus from 'element-plus'
import GenerateView from '../GenerateView.vue'

const {
  mockGetQuestionBanks,
  mockGenerateQuestionsApi,
  mockGenerateQuestionsAsyncApi,
  mockGetGenerationProgressApi,
  mockGetDataSourcesApi
} = vi.hoisted(() => ({
  mockGetQuestionBanks: vi.fn(),
  mockGenerateQuestionsApi: vi.fn(),
  mockGenerateQuestionsAsyncApi: vi.fn(),
  mockGetGenerationProgressApi: vi.fn(),
  mockGetDataSourcesApi: vi.fn()
}))

vi.mock('@/api/questionBank', () => ({
  getQuestionBanks: mockGetQuestionBanks
}))

vi.mock('@/api/dataSource', () => ({
  getDataSourcesApi: mockGetDataSourcesApi,
  createDataSourceApi: vi.fn(),
  updateDataSourceApi: vi.fn(),
  deleteDataSourceApi: vi.fn(),
  setDefaultDataSourceApi: vi.fn(),
  validateApiKeyApi: vi.fn()
}))

vi.mock('@/api/aiGeneration', () => ({
  generateQuestionsApi: mockGenerateQuestionsApi,
  generateQuestionsAsyncApi: mockGenerateQuestionsAsyncApi,
  getGenerationProgressApi: mockGetGenerationProgressApi,
  normalizeGeneratedQuestion: (question: unknown) => question
}))

describe('GenerateView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.setItem('token', 'test-token')
    setActivePinia(createPinia())

    mockGetDataSourcesApi.mockResolvedValue([
      {
        id: 1,
        name: 'Deepseek',
        type: 'deepseek',
        isDefault: true,
        createdAt: '2026-03-07T00:00:00.000Z',
        updatedAt: '2026-03-07T00:00:00.000Z'
      }
    ])

    mockGetQuestionBanks.mockResolvedValue({
      data: [
        {
          id: 5,
          userId: 1,
          name: '目标题库',
          description: 'desc',
          questionCount: 0,
          createdAt: '2026-03-07T00:00:00.000Z',
          updatedAt: '2026-03-07T00:00:00.000Z',
          tags: [],
          version: 'v1'
        }
      ]
    })

    mockGenerateQuestionsAsyncApi.mockResolvedValue({
      success: true,
      taskId: 'task-5',
      questions: []
    })

    mockGetGenerationProgressApi.mockResolvedValue({
      taskId: 'task-5',
      userId: 1,
      status: 'pending',
      generatedCount: 0,
      totalCount: 10,
      createdAt: '2026-03-07T00:00:00.000Z'
    })
  })

  it('应将路由中的 bankId 带入生成请求', async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [
        { path: '/generate', name: 'Generate', component: GenerateView },
        { path: '/ai-config', name: 'AIConfig', component: { template: '<div>AI Config</div>' } }
      ]
    })

    await router.push('/generate?bankId=5')
    await router.isReady()

    const wrapper = mount(GenerateView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    const subjectInput = wrapper.find('input[placeholder="例如：Vue 3 Composition API"]')
    await subjectInput.setValue('Vue 3 基础')
    await flushPromises()

    const generateButton = wrapper
      .findAll('button')
      .find(button => button.text().replace(/\s+/g, '').includes('开始生成'))

    expect(generateButton).toBeDefined()

    await generateButton!.trigger('click')
    await flushPromises()

    expect(mockGenerateQuestionsAsyncApi).toHaveBeenCalledWith(
      expect.objectContaining({
        questionBankId: 5,
        subject: 'Vue 3 基础'
      })
    )
  })
})
