import { defineStore } from 'pinia'
import { ref, computed } from 'vue'

export interface UserInfo {
  id: number
  username: string
  email: string
  createdAt?: string
}

export const useUserStore = defineStore('user', () => {
  // State
  const token = ref<string>('')
  const userInfo = ref<UserInfo | null>(null)

  // Getters
  const isLoggedIn = computed<boolean>(() => !!token.value && !!userInfo.value)

  // Actions
  function setToken(newToken: string) {
    token.value = newToken
  }

  function setUserInfo(info: UserInfo) {
    userInfo.value = info
  }

  function setAuth(newToken: string, info: UserInfo) {
    token.value = newToken
    userInfo.value = info
  }

  function clearAuth() {
    token.value = ''
    userInfo.value = null
  }

  function logout() {
    clearAuth()
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    setToken,
    setUserInfo,
    setAuth,
    clearAuth,
    logout
  }
}, {
  persist: {
    key: 'answerme-user',
    storage: localStorage,
    pick: ['token', 'userInfo']
  }
})
