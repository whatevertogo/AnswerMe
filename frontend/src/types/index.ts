// ==================== 题目类型定义 ====================
// 优先使用 @/types/question.ts 中的类型定义
// 以下为向后兼容保留的导出

import { QuestionType as NewQuestionType } from './question'
import type {
  Question as NewQuestion,
  QuestionData,
  ChoiceQuestionData,
  BooleanQuestionData,
  FillBlankQuestionData,
  ShortAnswerQuestionData,
  BaseQuestionData,
  CreateQuestionDto as NewCreateQuestionDto,
  UpdateQuestionDto as NewUpdateQuestionDto
} from './question'
import {
  getQuestionTypeLabel,
  getQuestionOptions,
  getQuestionCorrectAnswers,
  isMultipleChoiceQuestion,
  isSingleChoiceQuestion,
  isBooleanQuestion,
  isFillBlankQuestion,
  isShortAnswerQuestion,
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData
} from './question'

// 重新导出
export { NewQuestionType as QuestionType }
export type {
  QuestionData,
  ChoiceQuestionData,
  BooleanQuestionData,
  FillBlankQuestionData,
  ShortAnswerQuestionData,
  BaseQuestionData,
  NewCreateQuestionDto as CreateQuestionDto,
  NewUpdateQuestionDto as UpdateQuestionDto
}
export {
  getQuestionTypeLabel,
  getQuestionOptions,
  getQuestionCorrectAnswers,
  isMultipleChoiceQuestion,
  isSingleChoiceQuestion,
  isBooleanQuestion,
  isFillBlankQuestion,
  isShortAnswerQuestion,
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData
}

// 导出新 Question 类型
export type { NewQuestion as Question }

// 通用类型定义
export interface PaginationParams {
  page: number
  pageSize: number
}

export interface CursorPaginationParams {
  pageSize: number
  lastId?: number
}

export interface PaginatedResponse<T> {
  data: T[]
  total: number
  page: number
  pageSize: number
}

// API 响应类型
export interface ApiResponse<T = unknown> {
  success: boolean
  data: T
  message?: string
  errors?: Record<string, string[]>
}

// ==================== 用户认证相关类型 ====================

/**
 * 用户注册DTO
 */
export interface RegisterDto {
  username: string
  email: string
  password: string
}

/**
 * 用户登录DTO
 */
export interface LoginDto {
  email: string
  password: string
}

/**
 * 用户信息DTO
 */
export interface UserDto {
  id: number
  username: string
  email: string
  createdAt: string
}

/**
 * 认证响应DTO
 */
export interface AuthResponseDto {
  token: string
  user: UserDto
}

// ==================== 向后兼容的类型定义（已过时）====================
// TODO: 计划在下一个主版本中移除

/**
 * @deprecated 使用 QuestionType from './question' 代替
 */
export const LegacyQuestionType = {
  CHOICE: 'choice',
  MULTIPLE_CHOICE: 'multiple-choice',
  TRUE_FALSE: 'true-false',
  SHORT_ANSWER: 'short-answer'
} as const

/**
 * @deprecated 使用 QuestionType from './question' 代替
 */
export type QuestionTypeEnum = (typeof LegacyQuestionType)[keyof typeof LegacyQuestionType]

/**
 * @deprecated 使用 Difficulty from './question' 代替
 */
export const Difficulty = {
  EASY: 'easy',
  MEDIUM: 'medium',
  HARD: 'hard'
} as const

/**
 * @deprecated 使用 Difficulty from './question' 代替
 */
export type DifficultyEnum = (typeof Difficulty)[keyof typeof Difficulty]

/**
 * @deprecated 使用 Question interface from './question' 代替
 */
export interface QuestionLegacy {
  id: number
  content: string
  type: QuestionTypeEnum
  options?: string[]
  correctAnswer: string
  explanation?: string
  difficulty: DifficultyEnum
  tags: string[]
}

/** 题目查询参数 */
export interface QuestionQueryParams extends CursorPaginationParams {
  questionBankId?: number
  questionTypeEnum?: NewQuestionType
  difficulty?: string
  search?: string
}

/**
 * @deprecated 将在未来版本中更新为使用新的 QuestionData 结构
 */
/**
 * @deprecated 将在未来版本中更新为使用新的 QuestionData 结构
 */
/** @deprecated 使用 CreateQuestionDto from './question' 代替 */
export interface LegacyCreateQuestionDto {
  questionBankId: number
  content: string
  type: string
  options?: string[]
  correctAnswer: string
  explanation?: string
  difficulty: string
  tags?: string[]
}

/** @deprecated 使用 UpdateQuestionDto from './question' 代替 */
export interface LegacyUpdateQuestionDto {
  content?: string
  type?: string
  options?: string[]
  correctAnswer?: string
  explanation?: string
  difficulty?: string
  tags?: string[]
}

// 题库相关类型
export interface QuestionBank {
  id: number
  name: string
  description: string
  questionCount: number
  createdAt: string
  updatedAt: string
  version?: string
}

/** 题库列表响应 */
export interface QuestionBankListResponse {
  data: QuestionBank[]
  hasMore: boolean
  nextCursor?: number
}

/** 题库查询参数 */
export interface QuestionBankQueryParams {
  search?: string
  pageSize: number
  lastId?: number
}

/** 创建题库请求 */
export interface CreateQuestionBankDto {
  name: string
  description?: string
  tags?: string[]
  dataSourceId?: number
}

/** 更新题库请求 */
export interface UpdateQuestionBankDto {
  name?: string
  description?: string
  tags?: string[]
  dataSourceId?: number
  version?: string
}

// AI 配置相关类型
export interface AIConfig {
  id: string
  name: string
  provider: string
  apiKey: string
  apiUrl?: string
  model?: string
  isDefault: boolean
}

// 生成参数相关类型
export interface GenerateParams {
  questionBankId: number
  topic: string
  count: number
  difficulty: DifficultyEnum
  questionTypes: QuestionTypeEnum[]
  aiConfigId: number
}

// 答题相关类型
export interface QuizSession {
  id: number
  questionBankId: number
  questions: NewQuestion[]
  currentQuestionIndex: number
  answers: Map<number, string>
  startTime: string
  endTime?: string
}

export interface QuizResult {
  sessionId: string
  totalQuestions: number
  correctAnswers: number
  score: number
  timeSpent: number
  answers: Array<{
    questionId: string
    userAnswer: string
    correctAnswer: string
    isCorrect: boolean
  }>
}
