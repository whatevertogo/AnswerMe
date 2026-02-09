import api from './index'

/**
 * 开始答题
 */
export function startQuiz(data: {
  questionBankId: number
  mode?: 'sequential' | 'random'
}): Promise<any> {
  return api.post('/Attempts/start', data)
}

/**
 * 提交答案
 */
export function submitAnswer(data: {
  attemptId: number
  questionId: number
  userAnswer: string
  timeSpent?: number
}): Promise<any> {
  return api.post('/Attempts/submit-answer', data)
}

/**
 * 完成答题
 */
export function completeQuiz(data: {
  attemptId: number
}): Promise<any> {
  return api.post('/Attempts/complete', data)
}

/**
 * 获取答题记录详情
 */
export function getQuizResult(attemptId: string): Promise<any> {
  return api.get(`/Attempts/${attemptId}`)
}

/**
 * 获取答题详情列表
 */
export function getQuizDetails(attemptId: string): Promise<any> {
  return api.get(`/Attempts/${attemptId}/details`)
}

/**
 * 获取答题统计
 */
export function getQuizStatistics(): Promise<any> {
  return api.get('/Attempts/statistics')
}
