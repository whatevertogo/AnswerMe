import request from '@/utils/request'
import type {
  QuestionBank,
  QuestionBankListResponse,
  QuestionBankQueryParams,
  CreateQuestionBankDto,
  UpdateQuestionBankDto,
  Question
} from '@/types'

/**
 * 获取题库列表（游标分页）
 */
export function getQuestionBanks(params: QuestionBankQueryParams): Promise<QuestionBankListResponse> {
  return request.get<QuestionBankListResponse>('/QuestionBanks', { params })
}

/**
 * 获取题库详情
 */
export function getQuestionBankDetail(id: number): Promise<QuestionBank & { questions: Question[] }> {
  return request.get<QuestionBank & { questions: Question[] }>(`/QuestionBanks/${id}`)
}

/**
 * 搜索题库
 */
export function searchQuestionBanks(searchTerm: string): Promise<QuestionBank[]> {
  return request.get<QuestionBank[]>('/QuestionBanks/search', { params: { searchTerm } })
}

/**
 * 创建题库
 */
export function createQuestionBank(data: CreateQuestionBankDto): Promise<QuestionBank> {
  return request.post<QuestionBank>('/QuestionBanks', data)
}

/**
 * 更新题库
 */
export function updateQuestionBank(
  id: number,
  data: UpdateQuestionBankDto
): Promise<QuestionBank> {
  return request.put<QuestionBank>(`/QuestionBanks/${id}`, data)
}

/**
 * 删除题库
 */
export function deleteQuestionBank(id: number): Promise<{ message: string }> {
  return request.delete<{ message: string }>(`/QuestionBanks/${id}`)
}
