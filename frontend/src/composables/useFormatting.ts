/**
 * 格式化相关的组合式函数
 */

/**
 * 格式化日期字符串为本地化格式
 * @param dateString - ISO 日期字符串
 * @returns 格式化后的日期字符串 (zh-CN 格式)
 */
export const formatDate = (dateString: string | null | undefined): string => {
  if (!dateString) return '-'
  return new Date(dateString).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

/**
 * 格式化持续时间（秒）为可读格式
 * @param seconds - 秒数
 * @returns 格式化后的持续时间字符串
 */
export const formatDuration = (seconds: number | null | undefined): string => {
  if (seconds == null) return '-'

  if (seconds < 60) {
    return `${seconds}秒`
  }

  const minutes = Math.floor(seconds / 60)
  const remainingSeconds = seconds % 60

  if (minutes < 60) {
    return remainingSeconds > 0 ? `${minutes}分${remainingSeconds}秒` : `${minutes}分钟`
  }

  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60

  return remainingMinutes > 0 ? `${hours}小时${remainingMinutes}分钟` : `${hours}小时`
}
