import request from '@/utils/request'
import type { ApiResponse } from '@/types'

export interface LoginParams {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  userInfo: {
    id: number
    username: string
    email?: string
  }
}

export const loginApi = (params: LoginParams) => {
  return request<ApiResponse<LoginResponse>>({
    url: '/auth/login',
    method: 'POST',
    data: params
  })
}

export const logoutApi = () => {
  return request<ApiResponse>({
    url: '/auth/logout',
    method: 'POST'
  })
}

export const getUserInfoApi = () => {
  return request<ApiResponse<LoginResponse['userInfo']>>({
    url: '/auth/info',
    method: 'GET'
  })
}
