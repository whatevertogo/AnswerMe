import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { UserDto } from '@/types'

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

  // Computed
  const isAuthenticated = computed(() => !!token.value && !!userInfo.value)
  const userEmail = computed(() => userInfo.value?.email ?? '')

  // Actions
  function setToken(newToken: string) {
    token.value = newToken
    localStorage.setItem('token', newToken)
  }

  function setUserInfo(info: UserInfo) {
    userInfo.value = info
  }

  function setAuth(newToken: string, info: UserInfo) {
    token.value = newToken
    userInfo.value = info
    localStorage.setItem('token', newToken)
  }

  function setUser(userData: UserDto) {
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

  return {
    token,
    userInfo,
    isAuthenticated,
    userEmail,
    setToken,
    setUserInfo,
    setAuth,
    setUser,
    clearAuth,
    logout
  }
})
