import { request } from '@/utils/request'

/**
 * 错题查询参数
 */
export interface WrongQuestionQueryParams {
  questionBankId?: number
  questionType?: string
  startDate?: string
  endDate?: string
  pageIndex?: number
  pageSize?: number
}

/**
 * 错题详情
 */
export interface WrongQuestion {
  id: number
  questionId: number
  questionText: string
  questionType: string
  options?: string
  userAnswer: string
  correctAnswer: string
  explanation?: string
  attemptId: number
  answeredAt: string
  questionBankId: number
  questionBankName: string
  isMastered: boolean
}

/**
 * 错题列表响应
 */
export interface WrongQuestionListResponse {
  questions: WrongQuestion[]
  totalCount: number
  bankGroupCount: number
}

/**
 * 每周统计
 */
export interface WeeklyStat {
  weekStart: string
  attemptCount: number
  questionCount: number
  correctCount: number
  accuracyRate: number
}

/**
 * 题库统计
 */
export interface BankStat {
  questionBankId: number
  questionBankName: string
  attemptCount: number
  totalQuestions: number
  correctCount: number
  accuracyRate: number
}

/**
 * 学习统计响应
 */
export interface LearningStatsResponse {
  totalAttempts: number
  totalQuestions: number
  correctCount: number
  wrongCount: number
  accuracyRate: number
  averageTimePerQuestion: number
  totalTimeSpent: number
  weeklyTrend: WeeklyStat[]
  bankStats: BankStat[]
}

/**
 * 获取错题列表
 */
export function getWrongQuestions(
  params?: WrongQuestionQueryParams
): Promise<WrongQuestionListResponse> {
  return request.get('/wrong-questions', { params }) as Promise<WrongQuestionListResponse>
}

/**
 * 标记错题为已掌握
 */
export function markAsMastered(detailId: number): Promise<{ success: boolean; message: string }> {
  return request.post(`/wrong-questions/${detailId}/master`) as Promise<{
    success: boolean
    message: string
  }>
}

/**
 * 获取学习统计数据
 */
export function getLearningStats(): Promise<LearningStatsResponse> {
  return request.get('/wrong-questions/stats') as Promise<LearningStatsResponse>
}
