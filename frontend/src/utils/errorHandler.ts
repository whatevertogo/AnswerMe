/**
 * 统一错误处理工具
 */
export interface ErrorResponse {
  message?: string
  errorCode?: string
}

/**
 * 从错误对象中提取错误消息
 * @param err - 错误对象
 * @param fallback - 默认错误消息
 * @returns 提取的错误消息
 */
export function extractErrorMessage(err: any, fallback: string = '操作失败'): string {
  if (!err) return fallback

  // 尝试从响应中提取消息
  const errorMsg =
    err.response?.data?.message ||
    err.response?.data?.error?.message ||
    err.message ||
    fallback

  // 特殊错误码映射
  const errorMap: Record<string, string> = {
    NO_DATA_SOURCE: '未找到 AI 配置',
    INVALID_API_KEY: 'API Key 无效或未配置',
    UNAUTHORIZED: '未授权，请重新登录'
  }

  const errorCode = err.response?.data?.errorCode
  if (errorCode && errorMap[errorCode]) {
    return errorMap[errorCode]
  }

  // 特殊错误消息映射
  if (errorMsg.includes('Incorrect API key') ||
      errorMsg.includes('invalid_api_key') ||
      errorMsg.includes('401') ||
      errorMsg.includes('Unauthorized')) {
    return 'API Key 无效或未配置'
  }

  return errorMsg
}
