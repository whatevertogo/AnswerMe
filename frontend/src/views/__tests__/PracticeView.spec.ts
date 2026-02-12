import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createRouter, createMemoryHistory } from 'vue-router'
import ElementPlus from 'element-plus'
import PracticeView from '../PracticeView.vue'
import { useAuthStore } from '@/stores/auth'

const { mockGetQuestionBanks, mockGetQuestionBankDetail } = vi.hoisted(() => ({
  mockGetQuestionBanks: vi.fn(),
  mockGetQuestionBankDetail: vi.fn()
}))

vi.mock('@/api/questionBank', () => ({
  getQuestionBanks: mockGetQuestionBanks,
  getQuestionBankDetail: mockGetQuestionBankDetail,
  searchQuestionBanks: vi.fn(),
  createQuestionBank: vi.fn(),
  updateQuestionBank: vi.fn(),
  deleteQuestionBank: vi.fn()
}))

vi.mock('@/api/question', () => ({
  getQuestions: vi.fn()
}))

const createQuestionBank = (id: number, questionCount: number) => ({
  id,
  userId: 1,
  name: `Bank ${id}`,
  description: `Description ${id}`,
  questionCount,
  createdAt: '2026-01-01T00:00:00.000Z',
  updatedAt: '2026-01-01T00:00:00.000Z',
  tags: [],
  version: 'version-token'
})

const createTestRouter = () =>
  createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/practice', name: 'Practice', component: PracticeView },
      { path: '/quiz/:bankId/new', name: 'QuizNew', component: { template: '<div>Quiz</div>' } }
    ]
  })

describe('PracticeView', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    setActivePinia(createPinia())
  })

  it('加载更多应携带 nextCursor 并追加数据', async () => {
    const firstBank = createQuestionBank(1, 3)
    const secondBank = createQuestionBank(2, 4)

    mockGetQuestionBanks
      .mockResolvedValueOnce({
        data: [firstBank],
        hasMore: true,
        nextCursor: 1
      })
      .mockResolvedValueOnce({
        data: [secondBank],
        hasMore: false,
        nextCursor: undefined
      })

    const router = createTestRouter()
    await router.push('/practice')
    await router.isReady()

    const wrapper = mount(PracticeView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    await flushPromises()

    const loadMoreButton = wrapper
      .findAll('button')
      .find(button => button.text().replace(/\s+/g, '').includes('加载更多'))
    expect(loadMoreButton).toBeDefined()

    await loadMoreButton!.trigger('click')
    await flushPromises()

    expect(mockGetQuestionBanks).toHaveBeenNthCalledWith(2, {
      search: undefined,
      pageSize: 100,
      lastId: 1
    })
  })

  it('用户信息未初始化时仍可开始练习', async () => {
    const bank = createQuestionBank(3, 5)

    mockGetQuestionBanks.mockResolvedValueOnce({
      data: [bank],
      hasMore: false,
      nextCursor: undefined
    })
    mockGetQuestionBankDetail.mockResolvedValueOnce(bank)

    const router = createTestRouter()
    await router.push('/practice')
    await router.isReady()

    const wrapper = mount(PracticeView, {
      global: {
        plugins: [ElementPlus, router]
      }
    })

    const authStore = useAuthStore()
    authStore.setAuth('test-token', {
      id: 1,
      username: 'tester',
      email: 'tester@example.com'
    })
    authStore.userInfo = null

    await flushPromises()

    const startButton = wrapper
      .findAll('button')
      .find(button => button.text().replace(/\s+/g, '').includes('开始练习'))
    expect(startButton).toBeDefined()

    await startButton!.trigger('click')
    await flushPromises()

    expect(mockGetQuestionBankDetail).toHaveBeenCalledWith(3)
    expect(router.currentRoute.value.name).toBe('QuizNew')
    expect(router.currentRoute.value.params.bankId).toBe('3')
  })
})
