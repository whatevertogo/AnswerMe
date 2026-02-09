import { describe, it, expect, beforeEach, vi } from 'vitest'
import { authApi } from '../auth'
import request from '@/utils/request'
import type { LoginDto, RegisterDto, AuthResponseDto } from '@/types'

// Mock axios module
vi.mock('@/utils/request', () => ({
  default: {
    post: vi.fn(),
    get: vi.fn()
  }
}))

describe('Auth API', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('login应该调用正确的API端点', async () => {
    const mockLoginData: LoginDto = {
      email: 'test@example.com',
      password: 'password123'
    }

    const mockResponse: AuthResponseDto = {
      token: 'jwt-token',
      user: {
        id: 1,
        username: 'testuser',
        email: 'test@example.com',
        createdAt: '2024-01-01T00:00:00Z'
      }
    }

    vi.mocked(request.post).mockResolvedValue(mockResponse)

    await authApi.login(mockLoginData)

    expect(request.post).toHaveBeenCalledWith('/auth/login', mockLoginData)
  })

  it('register应该调用正确的API端点', async () => {
    const mockRegisterData: RegisterDto = {
      username: 'testuser',
      email: 'test@example.com',
      password: 'password123'
    }

    const mockResponse: AuthResponseDto = {
      token: 'jwt-token',
      user: {
        id: 1,
        username: 'testuser',
        email: 'test@example.com',
        createdAt: '2024-01-01T00:00:00Z'
      }
    }

    vi.mocked(request.post).mockResolvedValue(mockResponse)

    await authApi.register(mockRegisterData)

    expect(request.post).toHaveBeenCalledWith('/auth/register', mockRegisterData)
  })

  it('getCurrentUser应该调用正确的API端点', async () => {
    const mockUser = {
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      createdAt: '2024-01-01T00:00:00Z'
    }

    vi.mocked(request.get).mockResolvedValue(mockUser)

    await authApi.getCurrentUser()

    expect(request.get).toHaveBeenCalledWith('/auth/me')
  })
})
