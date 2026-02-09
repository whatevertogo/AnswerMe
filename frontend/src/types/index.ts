// 通用类型定义
export interface PaginationParams {
  page: number
  pageSize: number
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

// 题目相关类型
export const QuestionType = {
  CHOICE: 'choice',
  MULTIPLE_CHOICE: 'multiple-choice',
  TRUE_FALSE: 'true-false',
  SHORT_ANSWER: 'short-answer'
} as const

export type QuestionTypeEnum = (typeof QuestionType)[keyof typeof QuestionType]

export const Difficulty = {
  EASY: 'easy',
  MEDIUM: 'medium',
  HARD: 'hard'
} as const

export type DifficultyEnum = (typeof Difficulty)[keyof typeof Difficulty]

export interface Question {
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
export interface QuestionQueryParams extends PaginationParams {
  questionBankId?: number
  type?: string
  difficulty?: string
  search?: string
}

/** 创建题目DTO */
export interface CreateQuestionDto {
  questionBankId: number
  content: string
  type: string
  options?: string[]
  correctAnswer: string
  explanation?: string
  difficulty: string
  tags?: string[]
}

/** 更新题目DTO */
export interface UpdateQuestionDto {
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
  questions: Question[]
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
