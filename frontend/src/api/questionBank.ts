import { request } from '@/utils/request'
import type {
  QuestionBank,
  QuestionBankListResponse,
  QuestionBankQueryParams,
  CreateQuestionBankDto,
  UpdateQuestionBankDto
} from '@/types'

/**
 * 获取题库列表（游标分页）
 */
export function getQuestionBanks(
  params: QuestionBankQueryParams
): Promise<QuestionBankListResponse> {
  return request.get('/questionbanks', { params })
}

/**
 * 获取题库详情
 */
export function getQuestionBankDetail(id: number): Promise<QuestionBank> {
  return request.get(`/questionbanks/${id}`)
}

/**
 * 搜索题库
 */
export function searchQuestionBanks(searchTerm: string): Promise<QuestionBank[]> {
  return request.get('/questionbanks/search', { params: { searchTerm } })
}

/**
 * 创建题库
 */
export function createQuestionBank(data: CreateQuestionBankDto): Promise<QuestionBank> {
  return request.post('/questionbanks', data)
}

/**
 * 更新题库
 */
export function updateQuestionBank(id: number, data: UpdateQuestionBankDto): Promise<QuestionBank> {
  return request.put(`/questionbanks/${id}`, data)
}

/**
 * 删除题库
 */
export function deleteQuestionBank(id: number): Promise<{ message: string }> {
  return request.delete(`/questionbanks/${id}`)
}

function getQuestionBankExportPath(id: number): string {
  return `/questionbanks/${id}/export`
}

/**
 * 导出题库并下载文件
 * 使用 request 工具以复用统一鉴权拦截器
 */
export async function exportQuestionBankFile(id: number): Promise<void> {
  const blob = await request.get<Blob>(getQuestionBankExportPath(id), {
    responseType: 'blob'
  })
  const downloadUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = downloadUrl
  link.download = `questionbank_${new Date().getTime()}.json`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(downloadUrl)
}
