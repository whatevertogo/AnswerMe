import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { UserDto } from '@/types'
import { authApi } from '@/api/auth'

export interface UserInfo {
  id: number
  username: string
  email: string
  createdAt?: string
}

export const useAuthStore = defineStore('auth', () => {
  // State
  const token = ref<string | null>(localStorage.getItem('token'))
  const userInfo = ref<UserInfo | null>(null)
  const isLoading = ref(false)
  const isInitialized = ref(false)
  const loadError = ref<string | null>(null)

  // 单飞锁：防止并发重复请求
  let initializePromise: Promise<void> | null = null

  // Computed
  const isAuthenticated = computed(() => !!token.value)
  const isFullyAuthenticated = computed(() => !!token.value && !!userInfo.value)
  const userEmail = computed(() => userInfo.value?.email ?? '')

  // Actions
  function setAuth(newToken: string, info: UserInfo) {
    token.value = newToken
    userInfo.value = info
    localStorage.setItem('token', newToken)
  }

  function setUserFromDto(userData: UserDto) {
    userInfo.value = {
      id: userData.id,
      username: userData.username,
      email: userData.email,
      createdAt: userData.createdAt
    }
  }

  function clearAuth() {
    token.value = null
    userInfo.value = null
    localStorage.removeItem('token')
  }

  function logout() {
    clearAuth()
  }

  async function initialize(): Promise<void> {
    // 如果已经初始化过或没有 token，直接返回
    if (isInitialized.value || !token.value) {
      isInitialized.value = true
      return
    }

    // 单飞模式：如果有正在进行的初始化，返回同一个 Promise
    if (initializePromise) {
      return initializePromise
    }

    initializePromise = (async () => {
      isLoading.value = true
      loadError.value = null

      try {
        const userData = await authApi.getCurrentUser()
        userInfo.value = {
          id: userData.id,
          username: userData.username,
          email: userData.email,
          createdAt: userData.createdAt
        }
      } catch (error: any) {
        // 根据错误类型处理
        if (error?.response?.status === 401 || error?.response?.status === 403) {
          clearAuth() // 无效 token，清除
        } else {
          loadError.value = '网络错误，无法获取用户信息'
          // 保留 token，允许重试
        }
      } finally {
        isLoading.value = false
        isInitialized.value = true
        initializePromise = null
      }
    })()

    return initializePromise
  }

  async function retryFetchUserInfo(): Promise<void> {
    if (!token.value) return
    loadError.value = null
    isInitialized.value = false
    await initialize()
  }

  return {
    token,
    userInfo,
    isAuthenticated,
    isFullyAuthenticated,
    userEmail,
    isLoading,
    isInitialized,
    loadError,
    setAuth,
    setUserFromDto,
    clearAuth,
    logout,
    initialize,
    retryFetchUserInfo
  }
})
