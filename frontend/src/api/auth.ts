import type { AxiosResponse } from 'axios'
import api from './index'
import type { LoginDto, RegisterDto, AuthResponseDto, UserDto } from '@/types'

/**
 * 认证API
 */
export const authApi = {
  /**
   * 用户登录
   * POST /api/auth/login
   */
  login(data: LoginDto): Promise<AxiosResponse<AuthResponseDto>> {
    return api.post<AuthResponseDto>('/auth/login', data)
  },

  /**
   * 用户注册
   * POST /api/auth/register
   */
  register(data: RegisterDto): Promise<AxiosResponse<AuthResponseDto>> {
    return api.post<AuthResponseDto>('/auth/register', data)
  },

  /**
   * 获取当前用户信息
   * GET /api/auth/me
   */
  getCurrentUser(): Promise<AxiosResponse<UserDto>> {
    return api.get<UserDto>('/auth/me')
  },

  /**
   * 本地登录（无需账号密码）
   * POST /api/auth/local-login
   */
  localLogin(): Promise<AxiosResponse<AuthResponseDto>> {
    return api.post<AuthResponseDto>('/auth/local-login')
  }
}
