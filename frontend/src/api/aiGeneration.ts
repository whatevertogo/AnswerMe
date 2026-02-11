import { request } from '@/utils/request'
import type { QuestionType, QuestionData } from '@/types/question'

// 难度枚举（复用 question.ts 中的定义）
export type Difficulty = 'easy' | 'medium' | 'hard'

// 语言枚举
export type Language = 'zh-CN' | 'en-US'

// 任务状态
export type TaskStatus = 'pending' | 'processing' | 'completed' | 'failed' | 'partial_success'

/**
 * AI生成题目请求参数
 */
export interface AIGenerateRequest {
  /** 题库ID（可选，如果提供则将生成的题目添加到该题库） */
  questionBankId?: number
  /** 生成主题 */
  subject: string
  /** 生成数量 */
  count: number
  /** 难度等级 */
  difficulty: Difficulty
  /** 题型列表 */
  questionTypes: QuestionType[]
  /** 语言 */
  language: Language
  /** 自定义提示词（可选） */
  customPrompt?: string
  /** AI Provider名称（可选，默认使用第一个可用Provider） */
  providerName?: string
}

/**
 * 生成的题目
 */
export interface GeneratedQuestion {
  id: number
  /** 题型枚举（新格式，对应后端 QuestionTypeEnum） */
  questionTypeEnum?: QuestionType
  /** 题型（旧格式，向后兼容） */
  questionType?: QuestionType
  questionText: string
  /** 选项（旧格式，向后兼容） */
  options?: string[]
  /** 正确答案（旧格式，向后兼容） */
  correctAnswer?: string
  explanation?: string
  /** 难度等级 */
  difficulty: Difficulty
  questionBankId: number
  createdAt: string
  /** 题目数据（新格式） */
  data?: QuestionData
}

/**
 * 归一化生成的题目（兼容新旧格式）
 * 优先使用 questionTypeEnum，fallback 到 questionType
 */
export function normalizeGeneratedQuestion(raw: any): GeneratedQuestion {
  return {
    ...raw,
    // 优先使用 questionTypeEnum，fallback 到 questionType
    questionTypeEnum: raw.questionTypeEnum ?? raw.questionType,
    // 兼容层：确保两个字段都有值
    questionType: raw.questionType ?? raw.questionTypeEnum
  }
}

/**
 * AI生成题目响应
 */
export interface AIGenerateResponse {
  /** 是否成功 */
  success: boolean
  /** 生成的题目列表 */
  questions: GeneratedQuestion[]
  /** 任务ID（用于异步任务查询） */
  taskId?: string
  /** 错误消息 */
  errorMessage?: string
  /** 错误代码 */
  errorCode?: string
  /** 使用的Token数量 */
  tokensUsed?: number
  /** 部分成功：返回成功生成的题目数量 */
  partialSuccessCount?: number
}

/**
 * 生成进度查询响应
 */
export interface AIGenerateProgress {
  /** 任务ID */
  taskId: string
  /** 用户ID（后端用于权限验证） */
  userId: number
  /** 任务状态 */
  status: TaskStatus
  /** 已生成的题目数量 */
  generatedCount: number
  /** 总题目数量 */
  totalCount: number
  /** 错误消息（如果失败） */
  errorMessage?: string
  /** 已生成的题目（部分成功时返回） */
  questions?: GeneratedQuestion[]
  /** 创建时间 */
  createdAt: string
  /** 完成时间 */
  completedAt?: string
}

/**
 * 生成题目（同步，≤20题）
 */
export const generateQuestionsApi = (params: AIGenerateRequest): Promise<AIGenerateResponse> => {
  return request.post('/AIGeneration/generate', params)
}

/**
 * 生成题目（异步，>20题）
 */
export const generateQuestionsAsyncApi = (params: AIGenerateRequest): Promise<AIGenerateResponse> => {
  return request.post('/AIGeneration/generate-async', params)
}

/**
 * 查询生成进度
 */
export const getGenerationProgressApi = (taskId: string): Promise<AIGenerateProgress> => {
  return request.get(`/AIGeneration/progress/${taskId}`)
}
