import api from './index'
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
  return api.get('/QuestionBanks', { params }).then(res => res.data)
}

/**
 * 获取题库详情
 */
export function getQuestionBankDetail(id: number): Promise<QuestionBank & { questions: Question[] }> {
  return api.get(`/QuestionBanks/${id}`).then(res => res.data)
}

/**
 * 搜索题库
 */
export function searchQuestionBanks(searchTerm: string): Promise<QuestionBank[]> {
  return api.get('/QuestionBanks/search', { params: { searchTerm } }).then(res => res.data)
}

/**
 * 创建题库
 */
export function createQuestionBank(data: CreateQuestionBankDto): Promise<QuestionBank> {
  return api.post('/QuestionBanks', data).then(res => res.data)
}

/**
 * 更新题库
 */
export function updateQuestionBank(
  id: number,
  data: UpdateQuestionBankDto
): Promise<QuestionBank> {
  return api.put(`/QuestionBanks/${id}`, data).then(res => res.data)
}

/**
 * 删除题库
 */
export function deleteQuestionBank(id: number): Promise<{ message: string }> {
  return api.delete(`/QuestionBanks/${id}`).then(res => res.data)
}
