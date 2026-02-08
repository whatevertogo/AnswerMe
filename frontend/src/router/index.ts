import { createRouter, createWebHistory } from 'vue-router'
import type { RouteRecordRaw } from 'vue-router'
import { useUserStore } from '@/stores/user'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'Home',
    redirect: '/home'
  },
  {
    path: '/home',
    name: 'Dashboard',
    component: () => import('@/views/HomeView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/LoginView.vue'),
    meta: { requiresAuth: false, layout: 'auth' }
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/RegisterView.vue'),
    meta: { requiresAuth: false, layout: 'auth' }
  },
  {
    path: '/question-banks',
    name: 'QuestionBanks',
    component: () => import('@/views/QuestionBanksView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/question-banks/:id',
    name: 'QuestionBankDetail',
    component: () => import('@/views/QuestionBankDetailView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/data-sources',
    name: 'DataSources',
    component: () => import('@/views/DataSourcesView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/ai-config',
    name: 'AIConfig',
    component: () => import('@/views/AIConfigView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/generate',
    name: 'Generate',
    component: () => import('@/views/GenerateView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/quiz/:bankId/:sessionId',
    name: 'Quiz',
    component: () => import('@/views/QuizView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  },
  {
    path: '/result/:sessionId',
    name: 'Result',
    component: () => import('@/views/ResultView.vue'),
    meta: { requiresAuth: true, layout: 'app' }
  }
]

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes
})

// 路由守卫 - 使用Pinia store进行认证检查
router.beforeEach((to, _from, next) => {
  const userStore = useUserStore()
  const isLoggedIn = userStore.isLoggedIn
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
