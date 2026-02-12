import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosResponse,
  type InternalAxiosRequestConfig
} from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'

// TODO-开发环境使用vite代理，生产环境使用完整URL
const baseURL = import.meta.env.VITE_API_BASE_URL || '/api'

const instance: AxiosInstance = axios.create({
  baseURL,
  timeout: 120000, // ✅ 增加到120秒（2分钟），适应AI生成时间
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
instance.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // 从Pinia store获取token
    const authStore = useAuthStore()
    const token = authStore.token

    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }

    return config
  },
  error => {
    return Promise.reject(error)
  }
)

// 响应拦截器
instance.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  error => {
    const { response } = error
    let message = '请求失败'
    const showError = true // 控制是否显示错误消息

    if (response) {
      // 尝试从错误响应中提取消息（支持新旧两种格式）
      const errorMessage =
        response.data?.error?.message || // 新格式: { error: { message } }
        response.data?.message || // 旧格式: { message }
        '请求失败'

      switch (response.status) {
        case 400:
          message = errorMessage
          break
        case 401: {
          message = '登录已过期，请重新登录'
          // 清除用户状态
          const authStore = useAuthStore()
          authStore.clearAuth()
          // 跳转到登录页，避免循环
          if (router.currentRoute.value.path !== '/login') {
            router.push('/login')
          }
          break
        }
        case 403:
          message = '拒绝访问'
          break
        case 404:
          message = '请求资源不存在'
          break
        case 500:
          message = '服务器内部错误，请稍后重试'
          break
        case 502:
        case 503:
        case 504:
          message = '服务暂时不可用，请稍后重试'
          break
        default:
          message = errorMessage
      }
    } else if (error.code === 'ECONNABORTED') {
      message = '请求超时，请检查网络连接'
    } else if (error.message === 'Network Error') {
      message = '网络连接失败，请检查网络设置'
    } else if (error.message?.includes('timeout')) {
      message = '请求超时，请稍后重试'
    }

    // 只在有明确错误消息时显示
    if (showError && message) {
      ElMessage.error({
        message,
        duration: 3000,
        showClose: true
      })
    }

    return Promise.reject(error)
  }
)

export default instance

// 类型安全的请求方法
// 响应拦截器已经自动解包 response.data
export const request = {
  get: <T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> => {
    return instance.get<T>(url, config).then(res => res.data)
  },
  post: <T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> => {
    return instance.post<T>(url, data, config).then(res => res.data)
  },
  put: <T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> => {
    return instance.put<T>(url, data, config).then(res => res.data)
  },
  delete: <T = unknown>(url: string, config?: AxiosRequestConfig): Promise<T> => {
    return instance.delete<T>(url, config).then(res => res.data)
  },
  patch: <T = unknown>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> => {
    return instance.patch<T>(url, data, config).then(res => res.data)
  }
}
