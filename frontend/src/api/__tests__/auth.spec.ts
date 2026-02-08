import { describe, it, expect, beforeEach, vi } from 'vitest'
import { authApi } from '../auth'
import api from '../index'
import type { LoginDto, RegisterDto, AuthResponseDto } from '@/types'

// Mock axios module
vi.mock('../index', () => ({
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

    vi.mocked(api.post).mockResolvedValue({ data: mockResponse } as any)

    await authApi.login(mockLoginData)

    expect(api.post).toHaveBeenCalledWith('/auth/login', mockLoginData)
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

    vi.mocked(api.post).mockResolvedValue({ data: mockResponse } as any)

    await authApi.register(mockRegisterData)

    expect(api.post).toHaveBeenCalledWith('/auth/register', mockRegisterData)
  })

  it('getCurrentUser应该调用正确的API端点', async () => {
    const mockUser = {
      id: 1,
      username: 'testuser',
      email: 'test@example.com',
      createdAt: '2024-01-01T00:00:00Z'
    }

    vi.mocked(api.get).mockResolvedValue({ data: mockUser } as any)

    await authApi.getCurrentUser()

    expect(api.get).toHaveBeenCalledWith('/auth/me')
  })
})
