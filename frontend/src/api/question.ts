import api from './index'
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
  return api.get('/Questions', { params }).then(res => res.data)
}

/**
 * 获取题目详情
 */
export function getQuestionDetail(id: string): Promise<Question> {
  return api.get(`/Questions/${id}`).then(res => res.data)
}

/**
 * 创建题目
 */
export function createQuestion(data: CreateQuestionDto): Promise<Question> {
  return api.post('/Questions', data).then(res => res.data)
}

/**
 * 批量创建题目
 */
export function createQuestions(data: CreateQuestionDto[]): Promise<Question[]> {
  return api.post('/Questions/batch', data).then(res => res.data)
}

/**
 * 更新题目
 */
export function updateQuestion(
  id: string,
  data: UpdateQuestionDto
): Promise<Question> {
  return api.put(`/Questions/${id}`, data).then(res => res.data)
}

/**
 * 删除题目
 */
export function deleteQuestion(id: string): Promise<{ message: string }> {
  return api.delete(`/Questions/${id}`).then(res => res.data)
}

/**
 * 批量删除题目
 */
export function deleteQuestions(ids: string[]): Promise<{ message: string }> {
  return api.post('/Questions/batch-delete', ids).then(res => res.data)
}

/**
 * 搜索题目
 */
export function searchQuestions(searchTerm: string, questionBankId?: string): Promise<Question[]> {
  return api.get('/Questions/search', {
    params: { searchTerm, questionBankId }
  }).then(res => res.data)
}
