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
  return request.get('/questions', { params })
}

/**
 * 获取题目详情
 */
export function getQuestionDetail(id: number): Promise<Question> {
  return request.get<Question>(`/questions/${id}`)
}

/**
 * 创建题目
 */
export function createQuestion(data: CreateQuestionDto): Promise<Question> {
  return request.post<Question>('/questions', data)
}

/**
 * 批量创建题目
 */
export function createQuestions(data: CreateQuestionDto[]): Promise<Question[]> {
  return request.post<Question[]>('/questions/batch', data)
}

/**
 * 更新题目
 */
export function updateQuestion(
  id: number,
  data: UpdateQuestionDto
): Promise<Question> {
  return request.put<Question>(`/questions/${id}`, data)
}

/**
 * 删除题目
 */
export function deleteQuestion(id: number): Promise<{ message: string }> {
  return request.delete<{ message: string }>(`/questions/${id}`)
}

/**
 * 批量删除题目
 */
export function deleteQuestions(ids: number[]): Promise<{ message: string; successCount: number; notFoundCount: number }> {
  return request.post<{ message: string; successCount: number; notFoundCount: number }>('/questions/batch-delete', ids)
}

/**
 * 搜索题目
 */
export function searchQuestions(searchTerm: string, questionBankId?: number): Promise<Question[]> {
  return request.get<Question[]>('/questions/search', {
    params: { searchTerm, questionBankId }
  })
}
