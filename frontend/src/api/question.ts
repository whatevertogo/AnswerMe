import request from '@/utils/request'
import type { AxiosResponse } from 'axios'
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
  items: Question[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

/**
 * 获取题目列表（分页）
 */
export function getQuestions(params: QuestionQueryParams): Promise<AxiosResponse<QuestionListResponse>> {
  return request.get<QuestionListResponse>('/questions', { params })
}

/**
 * 获取题目详情
 */
export function getQuestionDetail(id: number): Promise<AxiosResponse<Question>> {
  return request.get<Question>(`/questions/${id}`)
}

/**
 * 创建题目
 */
export function createQuestion(data: CreateQuestionDto): Promise<AxiosResponse<Question>> {
  return request.post<Question>('/questions', data)
}

/**
 * 批量创建题目
 */
export function createQuestions(data: CreateQuestionDto[]): Promise<AxiosResponse<Question[]>> {
  return request.post<Question[]>('/questions/batch', data)
}

/**
 * 更新题目
 */
export function updateQuestion(
  id: number,
  data: UpdateQuestionDto
): Promise<AxiosResponse<Question>> {
  return request.put<Question>(`/questions/${id}`, data)
}

/**
 * 删除题目
 */
export function deleteQuestion(id: number): Promise<AxiosResponse<{ message: string }>> {
  return request.delete<{ message: string }>(`/questions/${id}`)
}

/**
 * 批量删除题目
 */
export function deleteQuestions(ids: number[]): Promise<AxiosResponse<{ message: string; successCount: number; notFoundCount: number }>> {
  return request.post<{ message: string; successCount: number; notFoundCount: number }>('/questions/batch-delete', ids)
}

/**
 * 搜索题目
 */
export function searchQuestions(searchTerm: string, questionBankId?: number): Promise<AxiosResponse<Question[]>> {
  return request.get<Question[]>('/questions/search', {
    params: { searchTerm, questionBankId }
  })
}
