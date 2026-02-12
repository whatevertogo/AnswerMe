import { describe, it, expect, beforeEach, vi } from 'vitest'
import {
  extractErrorMessage,
  isRetryableError,
  getErrorDetails,
  ErrorCategory
} from '../errorHandler'

describe('errorHandler', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('extractErrorMessage', () => {
    it('应从错误码提取已知的错误消息', () => {
      const err = {
        response: {
          data: {
            errorCode: 'UNAUTHORIZED',
            message: 'Some message'
          }
        }
      }
      expect(extractErrorMessage(err)).toBe('未授权，请重新登录')
    })

    it('应从响应中提取错误消息', () => {
      const err = {
        response: {
          data: {
            message: 'Custom error message'
          }
        }
      }
      expect(extractErrorMessage(err)).toBe('Custom error message')
    })

    it('应从错误对象中提取消息', () => {
      const err = {
        message: 'Direct error message'
      }
      expect(extractErrorMessage(err)).toBe('Direct error message')
    })

    it('应根据消息内容映射 API Key 错误', () => {
      const err = {
        message: 'incorrect api key provided'
      }
      expect(extractErrorMessage(err)).toBe('API Key 无效或未配置')
    })

    it('应根据消息内容映射 Token 过期错误', () => {
      const err = {
        message: 'Token has expired'
      }
      expect(extractErrorMessage(err)).toBe('登录已过期，请重新登录')
    })

    it('应根据消息内容映射网络错误', () => {
      const err = {
        message: 'Network request failed'
      }
      expect(extractErrorMessage(err)).toBe('网络连接失败，请检查网络')
    })

    it('应根据消息内容映射超时错误', () => {
      const err = {
        message: 'Request timeout'
      }
      expect(extractErrorMessage(err)).toBe('请求超时，请稍后重试')
    })

    it('应根据消息内容映射限流错误', () => {
      const err = {
        message: 'Rate limit exceeded'
      }
      expect(extractErrorMessage(err)).toBe('请求过于频繁，请稍后重试')
    })

    it('对未知错误使用 fallback 消息', () => {
      expect(extractErrorMessage(null, '默认消息')).toBe('默认消息')
      expect(extractErrorMessage(undefined, '测试')).toBe('测试')
    })

    it('应处理空错误对象', () => {
      expect(extractErrorMessage({}, 'fallback')).toBe('fallback')
    })
  })

  describe('isRetryableError', () => {
    it('应识别可重试的错误码', () => {
      const retryableCodes = ['RATE_LIMIT_EXCEEDED', 'TIMEOUT', 'SERVICE_UNAVAILABLE']
      retryableCodes.forEach(code => {
        const err = {
          response: {
            data: { errorCode: code }
          }
        }
        expect(isRetryableError(err)).toBe(true)
      })
    })

    it('应识别可重试的错误消息', () => {
      const retryableMessages = [
        'Request timeout',
        'Network error',
        'Rate limit exceeded',
        'Service unavailable'
      ]
      retryableMessages.forEach(msg => {
        const err = { message: msg }
        expect(isRetryableError(err)).toBe(true)
      })
    })

    it('不应将认证错误视为可重试', () => {
      const err = {
        response: {
          data: { errorCode: 'UNAUTHORIZED' }
        }
      }
      expect(isRetryableError(err)).toBe(false)
    })

    it('不应将验证错误视为可重试', () => {
      const err = {
        response: {
          data: { errorCode: 'VALIDATION_ERROR' }
        }
      }
      expect(isRetryableError(err)).toBe(false)
    })

    it('对 null 或 undefined 返回 false', () => {
      expect(isRetryableError(null)).toBe(false)
      expect(isRetryableError(undefined)).toBe(false)
    })
  })

  describe('getErrorDetails', () => {
    it('应提取完整的错误信息', () => {
      const err = {
        response: {
          data: {
            errorCode: 'RATE_LIMIT_EXCEEDED',
            message: 'Too many requests'
          }
        }
      }
      const details = getErrorDetails(err)
      expect(details.message).toBe('请求过于频繁，请稍后重试')
      expect(details.code).toBe('RATE_LIMIT_EXCEEDED')
      expect(details.retryable).toBe(true)
      expect(details.category).toBe('service')
    })

    it('应分类认证错误', () => {
      const err = {
        response: {
          data: { errorCode: 'UNAUTHORIZED' }
        }
      }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.AUTHENTICATION)
    })

    it('应分类授权错误', () => {
      const err = {
        response: {
          data: { errorCode: 'FORBIDDEN' }
        }
      }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.AUTHORIZATION)
    })

    it('应分类网络错误', () => {
      const err = { message: 'Network connection failed' }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.NETWORK)
    })

    it('应分类验证错误', () => {
      const err = {
        response: {
          data: { errorCode: 'VALIDATION_ERROR' }
        }
      }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.VALIDATION)
    })

    it('应分类服务错误', () => {
      const err = {
        response: {
          data: { errorCode: 'SERVICE_UNAVAILABLE' }
        }
      }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.SERVICE)
    })

    it('对未知错误分类为 UNKNOWN', () => {
      const err = { message: 'Something went wrong' }
      const details = getErrorDetails(err)
      expect(details.category).toBe(ErrorCategory.UNKNOWN)
    })

    it('处理 null 错误时返回默认分类', () => {
      const details = getErrorDetails(null)
      expect(details.category).toBe(ErrorCategory.UNKNOWN)
      expect(details.message).toBe('操作失败')
    })
  })

  describe('toErrorLike 转换', () => {
    it('应正确转换错误对象', () => {
      const err = {
        response: {
          status: 404,
          data: { errorCode: 'NOT_FOUND' }
        }
      }
      const details = getErrorDetails(err)
      expect(details.code).toBe('NOT_FOUND')
    })

    it('应处理字符串错误', () => {
      const err = 'Some string error message'
      const details = getErrorDetails(err)
      // 字符串错误会被转换为空对象，最终返回 fallback 消息
      expect(details.message).toBe('操作失败')
    })

    it('应处理空对象', () => {
      const err = {}
      const details = getErrorDetails(err)
      expect(details.code).toBeUndefined()
    })
  })
})
