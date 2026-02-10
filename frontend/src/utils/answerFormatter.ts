/**
 * 答案格式化工具类
 * 统一处理多选答案的数据格式转换
 */

/**
 * 将答案格式化为提交格式（JSON 数组字符串）
 * @param answer 答案（字符串或数组）
 * @returns JSON 数组字符串
 */
export function formatAnswerForSubmit(answer: string | string[]): string {
  if (Array.isArray(answer)) {
    return JSON.stringify(answer)
  }

  // 如果是 JSON 数组字符串，直接返回
  if (answer.trim().startsWith('[')) {
    return answer
  }

  // 如果是逗号分隔字符串，转换为数组再序列化
  if (answer.includes(',')) {
    return JSON.stringify(answer.split(',').map(a => a.trim()))
  }

  // 单个答案，包装为数组
  return JSON.stringify([answer])
}

/**
 * 将答案解析为数组格式
 * @param answer 答案（字符串或数组）
 * @returns 答案数组
 */
export function parseAnswerToArray(answer: string | string[] | undefined): string[] {
  if (!answer) return []

  if (Array.isArray(answer)) {
    return answer
  }

  // 如果是 JSON 数组字符串，解析
  const trimmed = answer.trim()
  if (trimmed.startsWith('[')) {
    try {
      return JSON.parse(trimmed)
    } catch {
      // 解析失败，按逗号分隔
      return answer.split(',').map(a => a.trim())
    }
  }

  // 逗号分隔字符串
  if (answer.includes(',')) {
    return answer.split(',').map(a => a.trim())
  }

  // 单个答案
  return [answer]
}

/**
 * 比较两个答案是否相同
 * @param answer1 答案1
 * @param answer2 答案2
 * @returns 是否相同
 */
export function compareAnswers(
  answer1: string | string[] | undefined,
  answer2: string | string[] | undefined
): boolean {
  const arr1 = parseAnswerToArray(answer1).sort()
  const arr2 = parseAnswerToArray(answer2).sort()

  if (arr1.length !== arr2.length) return false

  return arr1.every((val, index) => val === arr2[index])
}
