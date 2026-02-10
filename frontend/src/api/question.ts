import request from '@/utils/request'
import type {
  Question,
  QuestionQueryParams,
  CreateQuestionDto,
  UpdateQuestionDto
} from '@/types'

/**
 * 题目列表响应
 */
export interface QuestionListResponse {
  data: Question[]
  hasMore: boolean
  nextCursor?: number
  totalCount: number
}

/**
 * 获取题目列表（分页）
 */
export function getQuestions(params: QuestionQueryParams): Promise<QuestionListResponse> {
  return request.get('/questions', { params }) as Promise<QuestionListResponse>
}

/**
 * 获取题目详情
 */
export function getQuestionDetail(id: number): Promise<Question> {
  return request.get(`/questions/${id}`) as Promise<Question>
}

/**
 * 创建题目
 */
export function createQuestion(data: CreateQuestionDto): Promise<Question> {
  return request.post('/questions', data) as Promise<Question>
}

/**
 * 批量创建题目
 */
export function createQuestions(data: CreateQuestionDto[]): Promise<Question[]> {
  return request.post('/questions/batch', data) as Promise<Question[]>
}

/**
 * 更新题目
 */
export function updateQuestion(
  id: number,
  data: UpdateQuestionDto
): Promise<Question> {
  return request.put(`/questions/${id}`, data) as Promise<Question>
}

/**
 * 删除题目
 */
export function deleteQuestion(id: number): Promise<{ message: string }> {
  return request.delete(`/questions/${id}`) as Promise<{ message: string }>
}

/**
 * 批量删除题目
 */
export function deleteQuestions(ids: number[]): Promise<{ message: string; successCount: number; notFoundCount: number }> {
  return request.post('/questions/batch-delete', ids) as Promise<{ message: string; successCount: number; notFoundCount: number }>
}

/**
 * 搜索题目
 */
export function searchQuestions(searchTerm: string, questionBankId: number): Promise<Question[]> {
  return request.get('/questions/search', {
    params: { searchTerm, questionBankId }
  }) as Promise<Question[]>
}
