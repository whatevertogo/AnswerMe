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
  timeout: 15000,
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
    return response.data
  },
  error => {
    const { response } = error
    let message = '请求失败'

    if (response) {
      // 尝试从错误响应中提取消息（支持新旧两种格式）
      const errorMessage =
        response.data?.error?.message || // 新格式: { error: { message } }
        response.data?.message ||        // 旧格式: { message }
        '请求失败'

      switch (response.status) {
        case 400:
          message = errorMessage
          break
        case 401:
          message = '登录已过期，请重新登录'
          // 清除用户状态
          const authStore = useAuthStore()
          authStore.clearAuth()
          // 跳转到登录页
          router.push('/login')
          break
        case 403:
          message = '拒绝访问'
          break
        case 404:
          message = '请求资源不存在'
          break
        case 500:
          message = errorMessage
          break
        default:
          message = errorMessage
      }
    } else if (error.code === 'ECONNABORTED') {
      message = '请求超时'
    } else if (error.message === 'Network Error') {
      message = '网络连接失败，请检查网络'
    }

    ElMessage.error(message)
    return Promise.reject(error)
  }
)

export default instance

// 类型安全的请求方法
// 注意：响应拦截器已经自动解包 response.data，这里使用 as any 进行类型断言
export const request = {
  get: <T = any>(url: string, config?: AxiosRequestConfig): Promise<T> => {
    return instance.request({ ...config, method: 'GET', url }).then(res => res as unknown as T)
  },
  post: <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
    return instance.request({ ...config, method: 'POST', url, data }).then(res => res as unknown as T)
  },
  put: <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
    return instance.request({ ...config, method: 'PUT', url, data }).then(res => res as unknown as T)
  },
  delete: <T = any>(url: string, config?: AxiosRequestConfig): Promise<T> => {
    return instance.request({ ...config, method: 'DELETE', url }).then(res => res as unknown as T)
  },
  patch: <T = any>(url: string, data?: any, config?: AxiosRequestConfig): Promise<T> => {
    return instance.request({ ...config, method: 'PATCH', url, data }).then(res => res as unknown as T)
  }
}
