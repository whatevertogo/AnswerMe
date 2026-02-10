import request from '@/utils/request'
import type { AxiosResponse } from 'axios'

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
export function startQuiz(data: QuizStartParams): Promise<AxiosResponse<QuizStartResponse>> {
  return request.post<QuizStartResponse>('/attempts/start', data)
}

/**
 * 提交答案
 */
export function submitAnswer(data: QuizSubmitParams): Promise<AxiosResponse<boolean>> {
  return request.post<boolean>('/attempts/submit-answer', data)
}

/**
 * 完成答题
 */
export function completeQuiz(data: {
  attemptId: number
}): Promise<AxiosResponse<QuizResult>> {
  return request.post<QuizResult>('/attempts/complete', data)
}

/**
 * 获取答题记录详情
 */
export function getQuizResult(attemptId: number): Promise<AxiosResponse<QuizResult>> {
  return request.get<QuizResult>(`/attempts/${attemptId}`)
}

/**
 * 获取答题详情列表
 */
export function getQuizDetails(attemptId: number): Promise<AxiosResponse<QuizDetail[]>> {
  return request.get<QuizDetail[]>(`/attempts/${attemptId}/details`)
}

/**
 * 获取答题统计
 */
export function getQuizStatistics(): Promise<AxiosResponse<QuizStatistics>> {
  return request.get<QuizStatistics>('/attempts/statistics')
}
