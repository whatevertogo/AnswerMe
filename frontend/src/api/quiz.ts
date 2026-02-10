import request from '@/utils/request'

/**
 * 答题开始参数
 */
export interface QuizStartParams {
  questionBankId: number
  mode?: 'sequential' | 'random'
}

/**
 * 答题开始响应
 */
export interface QuizStartResponse {
  attemptId: number
  questionIds: number[]
}

/**
 * 提交答案参数
 */
export interface QuizSubmitParams {
  attemptId: number
  questionId: number
  userAnswer: string | string[]
  timeSpent?: number
}

/**
 * 答题结果
 */
export interface QuizResult {
  id: number
  questionBankId: number
  questionBankName: string
  startedAt?: string
  score: number
  totalQuestions: number
  correctCount: number
  durationSeconds?: number
  completedAt: string
}

/**
 * 答题详情
 */
export interface QuizDetail {
  id: number
  attemptId: number
  questionId: number
  questionText: string
  questionType: string
  options?: string
  userAnswer?: string | string[]
  correctAnswer: string
  isCorrect?: boolean
  timeSpent?: number
  explanation?: string
}

/**
 * 答题统计
 */
export interface QuizStatistics {
  totalAttempts: number
  totalQuestions: number
  averageScore: number
  averageTime: number
  completionRate: number
}

/**
 * 开始答题
 */
export function startQuiz(data: QuizStartParams): Promise<QuizStartResponse> {
  return request.post('/attempts/start', data)
}

/**
 * 提交答案
 */
export function submitAnswer(data: QuizSubmitParams): Promise<boolean> {
  return request.post('/attempts/submit-answer', data)
}

/**
 * 完成答题
 */
export function completeQuiz(data: {
  attemptId: number
}): Promise<QuizResult> {
  return request.post('/attempts/complete', data)
}

/**
 * 获取答题记录详情
 */
export function getQuizResult(attemptId: number): Promise<QuizResult> {
  return request.get(`/attempts/${attemptId}`)
}

/**
 * 获取答题详情列表
 */
export function getQuizDetails(attemptId: number): Promise<QuizDetail[]> {
  return request.get(`/attempts/${attemptId}/details`)
}

/**
 * 获取答题统计
 */
export function getQuizStatistics(): Promise<QuizStatistics> {
  return request.get('/attempts/statistics')
}
