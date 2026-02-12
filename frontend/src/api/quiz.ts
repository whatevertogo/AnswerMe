import { request } from '@/utils/request'

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
  userAnswer: string
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
  /** 分数（可能为null，表示未完成或未评分） */
  score: number | null
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
  completedAttempts: number
  totalQuestions: number
  totalCorrectAnswers: number
  averageScore: number
  overallAccuracy: number
}

export interface AttemptOverview {
  totalQuestions: number
  answeredQuestions: number
  correctQuestions: number
  incorrectQuestions: number
  unansweredQuestions: number
  accuracyRate: number
  durationSeconds: number
  averageTimePerAnswered: number
}

export interface AttemptWeakPoint {
  category: string
  name: string
  total: number
  incorrect: number
  accuracyRate: number
}

export interface AttemptAISuggestion {
  attemptId: number
  questionBankId: number
  questionBankName: string
  overview: AttemptOverview
  weakPoints: AttemptWeakPoint[]
  summary: string
  suggestions: string[]
  studyPlan: string
  providerName: string
  dataSourceName: string
  generatedAt: string
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
export function completeQuiz(data: { attemptId: number }): Promise<QuizResult> {
  return request.post('/attempts/complete', data)
}

/**
 * 获取答题记录详情
 */
export function getQuizResult(id: number): Promise<QuizResult> {
  return request.get(`/attempts/${id}`)
}

/**
 * 获取答题详情列表
 */
export function getQuizDetails(id: number): Promise<QuizDetail[]> {
  return request.get(`/attempts/${id}/details`)
}

/**
 * 获取答题统计
 */
export function getQuizStatistics(): Promise<QuizStatistics> {
  return request.get('/attempts/statistics')
}

/**
 * 生成答题 AI 学习建议
 */
export function generateAttemptAISuggestions(attemptId: number): Promise<AttemptAISuggestion> {
  return request.post(`/attempts/${attemptId}/ai-suggestions`)
}
