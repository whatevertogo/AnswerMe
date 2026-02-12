/**
 * 统一错误处理工具
 */

export interface ErrorResponse {
  message?: string
  errorCode?: string
}

interface ErrorLike {
  message?: string
  errorCode?: string
  response?: {
    status?: number
    data?: {
      message?: string
      errorCode?: string
      error?: {
        message?: string
      }
    }
  }
}

function toErrorLike(err: unknown): ErrorLike {
  if (typeof err === 'object' && err !== null) {
    return err as ErrorLike
  }
  return {}
}

/**
 * 可重试的错误码列表
 */
const RETRYABLE_ERROR_CODES = [
  'RATE_LIMIT_EXCEEDED',
  'TIMEOUT',
  'SERVICE_UNAVAILABLE',
  'NETWORK_ERROR',
  'TEMPORARY_FAILURE'
]

/**
 * 错误码到消息的映射表
 */
const ERROR_MESSAGE_MAP: Record<string, string> = {
  // 认证相关
  UNAUTHORIZED: '未授权，请重新登录',
  INVALID_TOKEN: '登录已过期，请重新登录',
  TOKEN_EXPIRED: '登录已过期，请重新登录',
  FORBIDDEN: '权限不足，无法访问',

  // 数据源相关
  NO_DATA_SOURCE: '未找到 AI 配置，请先配置 API Key',
  INVALID_API_KEY: 'API Key 无效或未配置',
  CONFIG_DECRYPTION_FAILED: '数据源配置解密失败，请检查配置',
  UNSUPPORTED_PROVIDER: '不支持的 AI 提供商',

  // 请求相关
  RATE_LIMIT_EXCEEDED: '请求过于频繁，请稍后重试',
  TIMEOUT: '请求超时，请稍后重试',
  SERVICE_UNAVAILABLE: '服务暂时不可用，请稍后重试',
  NETWORK_ERROR: '网络连接失败，请检查网络',

  // 验证相关
  VALIDATION_ERROR: '输入数据有误，请检查后重试',
  INVALID_SUBJECT: '生成主题不能为空',
  INVALID_COUNT: '生成数量必须大于0',
  COUNT_EXCEEDED: '题目数量超过限制，请使用异步生成',

  // 资源相关
  NOT_FOUND: '请求的资源不存在',
  QUESTIONBANK_NOT_FOUND: '题库不存在或无权访问',
  QUESTION_NOT_FOUND: '题目不存在',

  // AI 生成相关
  AI_GENERATION_FAILED: 'AI 生成失败，请稍后重试',
  INVALID_AI_RESPONSE: 'AI 响应格式不正确，请调整提示词或模型',
  MAX_RETRIES_EXCEEDED: 'AI 生成失败，已达到最大重试次数',

  // 通用错误
  UNKNOWN_ERROR: '未知错误，请稍后重试',
  INTERNAL_ERROR: '服务器内部错误'
}

/**
 * 从错误对象中提取错误消息
 * @param err - 错误对象
 * @param fallback - 默认错误消息
 * @returns 提取的错误消息
 */
export function extractErrorMessage(err: unknown, fallback: string = '操作失败'): string {
  if (!err) return fallback

  const normalizedErr = toErrorLike(err)

  // 尝试从响应中提取错误码
  const errorCode = normalizedErr.response?.data?.errorCode || normalizedErr.errorCode
  if (errorCode && ERROR_MESSAGE_MAP[errorCode]) {
    return ERROR_MESSAGE_MAP[errorCode]
  }

  // 尝试从响应中提取消息
  const errorMsg =
    normalizedErr.response?.data?.message ||
    normalizedErr.response?.data?.error?.message ||
    normalizedErr.message ||
    fallback

  // 根据错误消息内容进行映射
  return mapErrorMessage(errorMsg)
}

/**
 * 根据错误消息内容映射到友好的提示
 */
function mapErrorMessage(errorMsg: string): string {
  const msg = errorMsg.toLowerCase()

  // 认证错误
  if (
    msg.includes('incorrect api key') ||
    msg.includes('invalid_api_key') ||
    msg.includes('401') ||
    msg.includes('unauthorized')
  ) {
    return 'API Key 无效或未配置'
  }

  if (msg.includes('token') && (msg.includes('expired') || msg.includes('invalid'))) {
    return '登录已过期，请重新登录'
  }

  // 网络错误
  if (msg.includes('network') || msg.includes('fetch')) {
    return '网络连接失败，请检查网络'
  }

  if (msg.includes('timeout')) {
    return '请求超时，请稍后重试'
  }

  // 限流错误
  if (msg.includes('rate limit') || msg.includes('too many requests')) {
    return '请求过于频繁，请稍后重试'
  }

  return errorMsg
}

/**
 * 判断错误是否可重试
 * @param err - 错误对象
 * @returns 是否可重试
 */
export function isRetryableError(err: unknown): boolean {
  if (!err) return false

  const normalizedErr = toErrorLike(err)

  const errorCode = normalizedErr.response?.data?.errorCode || normalizedErr.errorCode
  if (errorCode && RETRYABLE_ERROR_CODES.includes(errorCode)) {
    return true
  }

  // 根据错误消息判断
  const msg = (normalizedErr.response?.data?.message || normalizedErr.message || '').toLowerCase()
  return (
    msg.includes('timeout') ||
    msg.includes('network') ||
    msg.includes('rate limit') ||
    msg.includes('too many requests') ||
    msg.includes('service unavailable')
  )
}

/**
 * 获取错误的详细信息
 * @param err - 错误对象
 * @returns 错误详情对象
 */
export function getErrorDetails(err: unknown): {
  message: string
  code?: string
  retryable: boolean
  category: ErrorCategory
} {
  const normalizedErr = toErrorLike(err)
  return {
    message: extractErrorMessage(err),
    code: normalizedErr.response?.data?.errorCode || normalizedErr.errorCode,
    retryable: isRetryableError(err),
    category: categorizeError(err)
  }
}

/**
 * 错误分类
 */
export const ErrorCategory = {
  AUTHENTICATION: 'authentication',
  AUTHORIZATION: 'authorization',
  NETWORK: 'network',
  VALIDATION: 'validation',
  SERVICE: 'service',
  UNKNOWN: 'unknown'
} as const

export type ErrorCategory = (typeof ErrorCategory)[keyof typeof ErrorCategory]

/**
 * 对错误进行分类
 */
function categorizeError(err: unknown): ErrorCategory {
  if (!err) return ErrorCategory.UNKNOWN

  const normalizedErr = toErrorLike(err)

  const errorCode = normalizedErr.response?.data?.errorCode || normalizedErr.errorCode
  const msg = (normalizedErr.response?.data?.message || normalizedErr.message || '').toLowerCase()

  // 认证错误
  if (
    errorCode === 'UNAUTHORIZED' ||
    errorCode === 'INVALID_TOKEN' ||
    errorCode === 'TOKEN_EXPIRED' ||
    msg.includes('unauthorized') ||
    msg.includes('token')
  ) {
    return ErrorCategory.AUTHENTICATION
  }

  // 授权错误
  if (errorCode === 'FORBIDDEN' || msg.includes('forbidden')) {
    return ErrorCategory.AUTHORIZATION
  }

  // 网络错误
  if (msg.includes('network') || msg.includes('timeout') || msg.includes('fetch')) {
    return ErrorCategory.NETWORK
  }

  // 验证错误
  if (errorCode?.startsWith('INVALID_') || errorCode === 'VALIDATION_ERROR') {
    return ErrorCategory.VALIDATION
  }

  // 服务错误
  if (errorCode === 'SERVICE_UNAVAILABLE' || errorCode === 'RATE_LIMIT_EXCEEDED') {
    return ErrorCategory.SERVICE
  }

  return ErrorCategory.UNKNOWN
}
