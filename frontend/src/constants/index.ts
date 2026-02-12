/**
 * 全局常量定义
 */

// ==================== API 相关 ====================
export const API_TIMEOUT = 120000 // 2分钟
export const API_RETRY_DELAY = 1000 // 1秒
export const API_MAX_RETRIES = 3

// ==================== 分页相关 ====================
export const DEFAULT_PAGE_SIZE = 20
export const MAX_PAGE_SIZE = 100
export const PAGE_SIZE_OPTIONS = [10, 20, 50, 100]

// ==================== 验证规则 ====================
export const VALIDATION_RULES = {
  // 题库名称
  QUESTION_BANK_NAME: {
    MIN_LENGTH: 2,
    MAX_LENGTH: 100
  },
  // 题目内容
  QUESTION_TEXT: {
    MIN_LENGTH: 5,
    MAX_LENGTH: 1000
  },
  // 用户名
  USERNAME: {
    MIN_LENGTH: 3,
    MAX_LENGTH: 50
  },
  // 密码
  PASSWORD: {
    MIN_LENGTH: 6,
    MAX_LENGTH: 100
  },
  // 邮箱
  EMAIL: {
    MAX_LENGTH: 100
  }
} as const

// ==================== AI 生成相关 ====================
export const AI_GENERATION = {
  MAX_SYNC_COUNT: 20, // 同步生成最大题目数
  BATCH_SIZE: 5, // 批量生成大小
  DEFAULT_COUNT: 10, // 默认生成题目数
  MAX_COUNT: 100 // 最大生成题目数
} as const

// ==================== 本地存储 Key ====================
export const STORAGE_KEYS = {
  THEME_MODE: 'theme-mode',
  QUIZ_MARKED_QUESTIONS: 'quiz_marked_questions',
  AUTH_TOKEN: 'auth_token',
  USER_INFO: 'user_info'
} as const

// ==================== 难度级别 ====================
export const DIFFICULTY_LEVELS = ['easy', 'medium', 'hard'] as const
export type DifficultyLevel = (typeof DIFFICULTY_LEVELS)[number]

// ==================== 题目类型 ====================
export const QUESTION_TYPES = ['single', 'multiple', 'boolean', 'fill', 'essay'] as const
export type QuestionType = (typeof QUESTION_TYPES)[number]

// ==================== 答题模式 ====================
export const QUIZ_MODES = ['sequential', 'random'] as const
export type QuizMode = (typeof QUIZ_MODES)[number]

// ==================== 时间相关 ====================
export const TIME_FORMAT = 'mm:ss'
export const DATETIME_FORMAT = 'YYYY-MM-DD HH:mm:ss'

// ==================== 文件大小限制 ====================
export const FILE_SIZE_LIMITS = {
  IMAGE: 5 * 1024 * 1024, // 5MB
  DOCUMENT: 10 * 1024 * 1024 // 10MB
} as const

// ==================== 正则表达式 ====================
export const REGEX_PATTERNS = {
  EMAIL: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
  URL: /^https?:\/\/.+/
} as const
