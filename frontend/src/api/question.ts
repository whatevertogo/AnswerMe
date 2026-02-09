import request from '@/utils/request'
import type {
  Question,
  QuestionQueryParams,
  CreateQuestionDto,
  UpdateQuestionDto
} from '@/types'

/**
 * 获取题目列表（分页）
 */
export function getQuestions(params: QuestionQueryParams): Promise<any> {
  return request.get('/Questions', { params })
}

/**
 * 获取题目详情
 */
export function getQuestionDetail(id: string): Promise<Question> {
  return request.get<Question>(`/Questions/${id}`)
}

/**
 * 创建题目
 */
export function createQuestion(data: CreateQuestionDto): Promise<Question> {
  return request.post<Question>('/Questions', data)
}

/**
 * 批量创建题目
 */
export function createQuestions(data: CreateQuestionDto[]): Promise<Question[]> {
  return request.post<Question[]>('/Questions/batch', data)
}

/**
 * 更新题目
 */
export function updateQuestion(
  id: string,
  data: UpdateQuestionDto
): Promise<Question> {
  return request.put<Question>(`/Questions/${id}`, data)
}

/**
 * 删除题目
 */
export function deleteQuestion(id: string): Promise<{ message: string }> {
  return request.delete<{ message: string }>(`/Questions/${id}`)
}

/**
 * 批量删除题目
 */
export function deleteQuestions(ids: string[]): Promise<{ message: string }> {
  return request.post<{ message: string }>('/Questions/batch-delete', ids)
}

/**
 * 搜索题目
 */
export function searchQuestions(searchTerm: string, questionBankId?: string): Promise<Question[]> {
  return request.get<Question[]>('/Questions/search', {
    params: { searchTerm, questionBankId }
  })
}
