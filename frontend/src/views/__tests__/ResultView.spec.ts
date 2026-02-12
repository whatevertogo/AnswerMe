import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createRouter, createMemoryHistory } from 'vue-router'
import { createPinia, setActivePinia } from 'pinia'
import ElementPlus from 'element-plus'
import ResultView from '../ResultView.vue'
import type { AttemptAISuggestion } from '@/api/quiz'

const { mockGetQuizResult, mockGetQuizDetails, mockGenerateAttemptAISuggestions } = vi.hoisted(
  () => ({
    mockGetQuizResult: vi.fn(),
    mockGetQuizDetails: vi.fn(),
    mockGenerateAttemptAISuggestions: vi.fn()
  })
)

vi.mock('@/api/quiz', () => ({
  getQuizResult: mockGetQuizResult,
  getQuizDetails: mockGetQuizDetails,
  generateAttemptAISuggestions: mockGenerateAttemptAISuggestions
}))

const createRouterForResult = () =>
  createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/result/:sessionId', name: 'Result', component: ResultView }]
  })

const buildSuggestion = (): AttemptAISuggestion => ({
  attemptId: 100,
  questionBankId: 2,
  questionBankName: 'Unity',
  overview: {
    totalQuestions: 3,
    answeredQuestions: 2,
    correctQuestions: 1,
    incorrectQuestions: 1,
    unansweredQuestions: 1,
    accuracyRate: 33.33,
    durationSeconds: 60,
    averageTimePerAnswered: 20
  },
  weakPoints: [
    {
      category: '题型',
      name: '判断题',
      total: 2,
      incorrect: 1,
      accuracyRate: 50
    }
  ],
  summary: '这是AI复盘总结',
  suggestions: ['先做错题回顾', '每天练习10题'],
  studyPlan: '第1天复盘错题；第2天专项训练',
  providerName: 'DeepSeek',
  dataSourceName: 'Deepseek',
  generatedAt: '2026-02-12T08:00:00.000Z'
})

describe('ResultView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
  })

  it('应支持按错题筛选详情列表', async () => {
    mockGetQuizResult.mockResolvedValue({
      id: 100,
      questionBankId: 2,
      questionBankName: 'Unity',
      score: 33.33,
      totalQuestions: 3,
      correctCount: 1,
      durationSeconds: 60,
      completedAt: '2026-02-12T08:00:00.000Z'
    })

    mockGetQuizDetails.mockResolvedValue([
      {
        id: 1,
        attemptId: 100,
        questionId: 11,
        questionText: 'Q1',
        questionType: 'SingleChoice',
        userAnswer: 'A',
        correctAnswer: 'A',
        isCorrect: true
      },
      {
        id: 2,
        attemptId: 100,
        questionId: 12,
        questionText: 'Q2',
        questionType: 'SingleChoice',
        userAnswer: 'B',
        correctAnswer: 'A',
        isCorrect: false
      },
      {
        id: 3,
        attemptId: 100,
        questionId: 13,
        questionText: 'Q3',
        questionType: 'SingleChoice',
        userAnswer: '   ',
        correctAnswer: 'C',
        isCorrect: false
      }
    ])

    const router = createRouterForResult()
    await router.push('/result/100')
    await router.isReady()

    const wrapper = mount(ResultView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    expect(wrapper.findAll('.detail-item').length).toBe(3)
    const thirdDetail = wrapper.findAll('.detail-item')[2]
    expect(thirdDetail).toBeDefined()
    if (thirdDetail) {
      expect(thirdDetail.text()).toContain('未作答')
      expect(thirdDetail.text()).not.toContain('错误')
    }

    const incorrectFilterInput = wrapper.find('input[value="incorrect"]')
    expect(incorrectFilterInput.exists()).toBe(true)

    await incorrectFilterInput.setValue(true)
    await flushPromises()

    expect(wrapper.findAll('.detail-item').length).toBe(1)
  })

  it('应能生成并展示AI建议', async () => {
    mockGetQuizResult.mockResolvedValue({
      id: 100,
      questionBankId: 2,
      questionBankName: 'Unity',
      score: 80,
      totalQuestions: 5,
      correctCount: 4,
      durationSeconds: 120,
      completedAt: '2026-02-12T08:00:00.000Z'
    })
    mockGetQuizDetails.mockResolvedValue([])
    mockGenerateAttemptAISuggestions.mockResolvedValue(buildSuggestion())

    const router = createRouterForResult()
    await router.push('/result/100')
    await router.isReady()

    const wrapper = mount(ResultView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    const generateButton = wrapper
      .findAll('button')
      .find(button => button.text().replace(/\s+/g, '').includes('生成AI建议'))
    expect(generateButton).toBeDefined()

    await generateButton!.trigger('click')
    await flushPromises()

    expect(mockGenerateAttemptAISuggestions).toHaveBeenCalledWith(100)
    expect(wrapper.text()).toContain('这是AI复盘总结')
    expect(wrapper.text()).toContain('先做错题回顾')
  })

  it('重新生成AI建议失败时应清空旧内容并展示错误', async () => {
    mockGetQuizResult.mockResolvedValue({
      id: 100,
      questionBankId: 2,
      questionBankName: 'Unity',
      score: 80,
      totalQuestions: 5,
      correctCount: 4,
      durationSeconds: 120,
      completedAt: '2026-02-12T08:00:00.000Z'
    })
    mockGetQuizDetails.mockResolvedValue([])
    mockGenerateAttemptAISuggestions
      .mockResolvedValueOnce(buildSuggestion())
      .mockRejectedValueOnce(new Error('AI服务异常'))

    const router = createRouterForResult()
    await router.push('/result/100')
    await router.isReady()

    const wrapper = mount(ResultView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    const findButton = (label: string) =>
      wrapper.findAll('button').find(button => button.text().replace(/\s+/g, '').includes(label))

    const generateButton = findButton('生成AI建议')
    expect(generateButton).toBeDefined()
    await generateButton!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('这是AI复盘总结')
    const regenerateButton = findButton('重新生成建议')
    expect(regenerateButton).toBeDefined()

    await regenerateButton!.trigger('click')
    await flushPromises()

    expect(wrapper.text()).toContain('AI服务异常')
    expect(wrapper.text()).not.toContain('这是AI复盘总结')
    expect(findButton('复制建议')).toBeUndefined()
  })

  it('应支持高耗时筛选并可重置筛选条件', async () => {
    mockGetQuizResult.mockResolvedValue({
      id: 101,
      questionBankId: 2,
      questionBankName: 'Unity',
      score: 66.67,
      totalQuestions: 3,
      correctCount: 2,
      durationSeconds: 120,
      completedAt: '2026-02-12T08:00:00.000Z'
    })

    mockGetQuizDetails.mockResolvedValue([
      {
        id: 1,
        attemptId: 101,
        questionId: 21,
        questionText: 'Q1',
        questionType: 'SingleChoice',
        userAnswer: 'A',
        correctAnswer: 'A',
        isCorrect: true,
        timeSpent: 15
      },
      {
        id: 2,
        attemptId: 101,
        questionId: 22,
        questionText: 'Q2',
        questionType: 'SingleChoice',
        userAnswer: 'B',
        correctAnswer: 'A',
        isCorrect: false,
        timeSpent: 45
      },
      {
        id: 3,
        attemptId: 101,
        questionId: 23,
        questionText: 'Q3',
        questionType: 'SingleChoice',
        userAnswer: '',
        correctAnswer: 'C',
        isCorrect: false,
        timeSpent: 0
      }
    ])

    const router = createRouterForResult()
    await router.push('/result/101')
    await router.isReady()

    const wrapper = mount(ResultView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    expect(wrapper.findAll('.detail-item').length).toBe(3)

    const slowFilterInput = wrapper.find('input[value="slow"]')
    expect(slowFilterInput.exists()).toBe(true)

    await slowFilterInput.setValue(true)
    await flushPromises()

    const visibleItemsAfterSlowFilter = wrapper.findAll('.detail-item')
    expect(visibleItemsAfterSlowFilter.length).toBe(1)
    const firstVisibleItem = visibleItemsAfterSlowFilter[0]
    expect(firstVisibleItem).toBeDefined()
    if (firstVisibleItem) {
      expect(firstVisibleItem.text()).toContain('Q2')
    }

    const searchInput = wrapper.find('input[placeholder="搜索题干/答案"]')
    expect(searchInput.exists()).toBe(true)
    await searchInput.setValue('not-found')
    await flushPromises()
    expect(wrapper.findAll('.detail-item').length).toBe(0)

    const resetButton = wrapper
      .findAll('button')
      .find(button => button.text().replace(/\s+/g, '').includes('重置筛选'))
    expect(resetButton).toBeDefined()
    await resetButton!.trigger('click')
    await flushPromises()

    expect(wrapper.findAll('.detail-item').length).toBe(3)
    expect((searchInput.element as HTMLInputElement).value).toBe('')
  })
})
