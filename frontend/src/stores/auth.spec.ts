import { describe, it, expect, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from './auth'

describe('Auth Store', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
  })

  it('初始状态应该是未认证', () => {
    const authStore = useAuthStore()
    expect(authStore.isAuthenticated).toBe(false)
    expect(authStore.token).toBeNull()
    expect(authStore.user).toBeNull()
  })

  it('setToken应该更新token并保存到localStorage', () => {
    const authStore = useAuthStore()
    const testToken = 'test-jwt-token'

    authStore.setToken(testToken)

    expect(authStore.token).toBe(testToken)
    expect(localStorage.getItem('token')).toBe(testToken)
  })

  it('setUser应该更新用户信息', () => {
    const authStore = useAuthStore()
    const testUser = {
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      createdAt: new Date().toISOString()
    }

    authStore.setUser(testUser)

    expect(authStore.user).toEqual(testUser)
  })

  it('logout应该清除token和用户信息', () => {
    const authStore = useAuthStore()

    // 先设置token和用户
    authStore.setToken('test-token')
    authStore.setUser({
      id: 1,
      username: 'test',
      email: 'test@test.com',
      createdAt: new Date().toISOString()
    })

    // 执行logout
    authStore.logout()

    expect(authStore.token).toBeNull()
    expect(authStore.user).toBeNull()
    expect(localStorage.getItem('token')).toBeNull()
  })
})
