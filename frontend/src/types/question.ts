/**
 * 题型枚举
 *
 * 与后端 AnswerMe.Domain.Enums.QuestionType 保持一致
 */
export const QuestionType = {
  /** 单选题 */
  SingleChoice: 'SingleChoice',
  /** 多选题 */
  MultipleChoice: 'MultipleChoice',
  /** 判断题 */
  TrueFalse: 'TrueFalse',
  /** 填空题 */
  FillBlank: 'FillBlank',
  /** 简答题 */
  ShortAnswer: 'ShortAnswer'
} as const

export type QuestionType = (typeof QuestionType)[keyof typeof QuestionType]

/**
 * 题型显示名称映射
 */
export const QuestionTypeLabels: Record<QuestionType, string> = {
  [QuestionType.SingleChoice]: '单选题',
  [QuestionType.MultipleChoice]: '多选题',
  [QuestionType.TrueFalse]: '判断题',
  [QuestionType.FillBlank]: '填空题',
  [QuestionType.ShortAnswer]: '简答题'
}

/**
 * 获取题型显示名称
 */
export function getQuestionTypeLabel(type: QuestionType): string {
  return QuestionTypeLabels[type] || type
}

/**
 * 难度枚举
 */
export type Difficulty = 'easy' | 'medium' | 'hard'

/**
 * 难度显示名称映射
 */
export const DifficultyLabels: Record<Difficulty, string> = {
  easy: '简单',
  medium: '中等',
  hard: '困难'
}

/**
 * 题目数据基类
 *
 * 与后端 AnswerMe.Domain.Models.QuestionData 对应
 */
export interface BaseQuestionData {
  /** 题目解析 */
  explanation?: string
  /** 难度等级 */
  difficulty?: Difficulty
}

/**
 * 选择题数据（单选/多选）
 *
 * 与后端 ChoiceQuestionData 对应
 */
export interface ChoiceQuestionData extends BaseQuestionData {
  /** 选项列表，例如 ["A. 选项1", "B. 选项2"] */
  options: string[]
  /** 正确答案列表，单选时只有一个元素，多选时有多个 */
  correctAnswers: string[]
  /** 类型标识 */
  type: 'ChoiceQuestionData'
}

/**
 * 判断题数据
 *
 * 与后端 BooleanQuestionData 对应
 */
export interface BooleanQuestionData extends BaseQuestionData {
  /** 正确答案 */
  correctAnswer: boolean
  /** 类型标识 */
  type: 'BooleanQuestionData'
}

/**
 * 填空题数据
 *
 * 与后端 FillBlankQuestionData 对应
 */
export interface FillBlankQuestionData extends BaseQuestionData {
  /** 可接受的答案列表（支持多种表达方式） */
  acceptableAnswers: string[]
  /** 类型标识 */
  type: 'FillBlankQuestionData'
}

/**
 * 简答题数据
 *
 * 与后端 ShortAnswerQuestionData 对应
 */
export interface ShortAnswerQuestionData extends BaseQuestionData {
  /** 参考答案 */
  referenceAnswer: string
  /** 类型标识 */
  type: 'ShortAnswerQuestionData'
}

/**
 * 所有题型数据的联合类型
 */
export type QuestionData =
  | ChoiceQuestionData
  | BooleanQuestionData
  | FillBlankQuestionData
  | ShortAnswerQuestionData

/**
 * 题目实体
 *
 * 与后端 Question 实体对应
 * 只使用新字段，旧字段已废弃
 */
export interface Question {
  /** 题目ID */
  id: number
  /** 题目内容 */
  questionText: string
  /** 题型枚举 */
  questionTypeEnum: QuestionType
  /** 题目数据（新格式） */
  data?: QuestionData
  /** 题目解析 */
  explanation?: string
  /** 难度等级 */
  difficulty: Difficulty
  /** 题目标签 */
  tags?: string[]
  /** 所属题库ID */
  questionBankId?: number
  /** 创建时间 */
  createdAt?: string
}

/**
 * 创建题目DTO（新字段）
 */
export interface CreateQuestionDto {
  questionBankId: number
  questionText: string
  questionTypeEnum: QuestionType
  data?: QuestionData
  explanation?: string
  difficulty?: Difficulty
  orderIndex?: number
}

/**
 * 更新题目DTO（新字段）
 */
export interface UpdateQuestionDto {
  questionText?: string
  questionTypeEnum?: QuestionType
  data?: QuestionData
  explanation?: string
  difficulty?: Difficulty
  orderIndex?: number
}

/**
 * 类型守卫：判断是否为选择题数据
 */
export function isChoiceQuestionData(data: QuestionData | undefined): data is ChoiceQuestionData {
  return data?.type === 'ChoiceQuestionData'
}

/**
 * 类型守卫：判断是否为判断题数据
 */
export function isBooleanQuestionData(data: QuestionData | undefined): data is BooleanQuestionData {
  return data?.type === 'BooleanQuestionData'
}

/**
 * 类型守卫：判断是否为填空题数据
 */
export function isFillBlankQuestionData(data: QuestionData | undefined): data is FillBlankQuestionData {
  return data?.type === 'FillBlankQuestionData'
}

/**
 * 类型守卫：判断是否为简答题数据
 */
export function isShortAnswerQuestionData(data: QuestionData | undefined): data is ShortAnswerQuestionData {
  return data?.type === 'ShortAnswerQuestionData'
}

/**
 * 从题目数据中提取选项列表
 * 只使用新格式 data 属性
 */
export function getQuestionOptions(question: Question): string[] {
  if (isChoiceQuestionData(question.data)) {
    return question.data.options
  }
  return []
}

/**
 * 从题目数据中提取正确答案
 * 只使用新格式 data 属性
 */
export function getQuestionCorrectAnswers(question: Question): string[] | boolean | string {
  if (isChoiceQuestionData(question.data)) {
    return question.data.correctAnswers
  }
  if (isBooleanQuestionData(question.data)) {
    return question.data.correctAnswer
  }
  if (isFillBlankQuestionData(question.data)) {
    return question.data.acceptableAnswers
  }
  if (isShortAnswerQuestionData(question.data)) {
    return question.data.referenceAnswer
  }
  return ''
}

/**
 * 判断题目是否为多选题
 */
export function isMultipleChoiceQuestion(question: Question): boolean {
  return question.questionType === QuestionType.MultipleChoice
}

/**
 * 判断题目是否为单选题
 */
export function isSingleChoiceQuestion(question: Question): boolean {
  return question.questionType === QuestionType.SingleChoice
}

/**
 * 判断题目是否为判断题
 */
export function isBooleanQuestion(question: Question): boolean {
  return question.questionType === QuestionType.TrueFalse
}

/**
 * 判断题目是否为填空题
 */
export function isFillBlankQuestion(question: Question): boolean {
  return question.questionType === QuestionType.FillBlank
}

/**
 * 判断题目是否为简答题
 */
export function isShortAnswerQuestion(question: Question): boolean {
  return question.questionType === QuestionType.ShortAnswer
}
