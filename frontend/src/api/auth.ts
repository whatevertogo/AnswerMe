import { request } from '@/utils/request'
import type { LoginDto, RegisterDto, AuthResponseDto, UserDto } from '@/types'

/**
 * 认证API
 */
export const authApi = {
  /**
   * 用户登录
   * POST /api/auth/login
   */
  login(data: LoginDto): Promise<AuthResponseDto> {
    return request.post('/auth/login', data)
  },

  /**
   * 用户注册
   * POST /api/auth/register
   */
  register(data: RegisterDto): Promise<AuthResponseDto> {
    return request.post('/auth/register', data)
  },

  /**
   * 获取当前用户信息
   * GET /api/auth/me
   */
  getCurrentUser(): Promise<UserDto> {
    return request.get('/auth/me')
  },

  /**
   * 本地登录（无需账号密码）
   * POST /api/auth/local-login
   */
  localLogin(): Promise<AuthResponseDto> {
    return request.post('/auth/local-login')
  }
}
