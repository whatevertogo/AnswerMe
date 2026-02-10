import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

// Helper function to create authenticated routes
function authRoute(path: string, name: string, component: () => Promise<any>): RouteRecordRaw {
  return {
    path,
    name,
    component,
    meta: { requiresAuth: true, layout: 'app' }
  }
}

// Helper function to create public routes
function publicRoute(path: string, name: string, component: () => Promise<any>, layout: 'auth' | 'app' = 'auth'): RouteRecordRaw {
  return {
    path,
    name,
    component,
    meta: { requiresAuth: false, layout }
  }
}

const routes: Array<RouteRecordRaw> = [
  { path: '/', name: 'Home', redirect: '/home' },
  authRoute('/home', 'Dashboard', () => import('@/views/HomeView.vue')),
  publicRoute('/login', 'Login', () => import('@/views/LoginView.vue')),
  publicRoute('/register', 'Register', () => import('@/views/RegisterView.vue')),
  authRoute('/question-banks', 'QuestionBanks', () => import('@/views/QuestionBanksView.vue')),
  authRoute('/question-banks/:id', 'QuestionBankDetail', () => import('@/views/QuestionBankDetailView.vue')),
  authRoute('/questions', 'Questions', () => import('@/views/QuestionsView.vue')),
  authRoute('/question-banks/:bankId/questions', 'BankQuestions', () => import('@/views/QuestionsView.vue')),
  authRoute('/practice', 'Practice', () => import('@/views/PracticeView.vue')),
  authRoute('/ai-config', 'AIConfig', () => import('@/views/AIConfigView.vue')),
  authRoute('/generate', 'Generate', () => import('@/views/GenerateView.vue')),
  // 新答题路由：使用固定的 'new' 路径
  authRoute('/quiz/:bankId/new', 'QuizNew', () => import('@/views/QuizView.vue')),
  // 已有答题会话路由
  authRoute('/quiz/:bankId/:sessionId', 'Quiz', () => import('@/views/QuizView.vue')),
  authRoute('/result/:sessionId', 'Result', () => import('@/views/ResultView.vue'))
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

// 路由守卫 - 使用 authStore 进行认证检查
router.beforeEach(async (to, _from, next) => {
  const authStore = useAuthStore()

  // 等待初始化完成（如果有 token）
  if (!authStore.isInitialized) {
    await authStore.initialize()
  }

  const isLoggedIn = authStore.isAuthenticated
  const requiresAuth = to.meta.requiresAuth !== false

  if (requiresAuth && !isLoggedIn) {
    // 需要认证但未登录，跳转到登录页
    next('/login')
  } else if ((to.path === '/login' || to.path === '/register') && isLoggedIn) {
    // 已登录用户访问登录/注册页，跳转到首页
    next('/home')
  } else {
    next()
  }
})

export default router
