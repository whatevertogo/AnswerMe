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
export function getQuestionBanks(params: QuestionBankQueryParams): Promise<QuestionBankListResponse> {
  return request.get<QuestionBankListResponse>('/questionbanks', { params }).then(response => response.data)
}

/**
 * 获取题库详情
 */
export function getQuestionBankDetail(id: number): Promise<QuestionBank> {
  return request.get<QuestionBank>(`/questionbanks/${id}`).then(response => response.data)
}

/**
 * 搜索题库
 */
export function searchQuestionBanks(searchTerm: string): Promise<QuestionBank[]> {
  return request.get<QuestionBank[]>('/questionbanks/search', { params: { searchTerm } }).then(response => response.data)
}

/**
 * 创建题库
 */
export function createQuestionBank(data: CreateQuestionBankDto): Promise<QuestionBank> {
  return request.post<QuestionBank>('/questionbanks', data).then(response => response.data)
}

/**
 * 更新题库
 */
export function updateQuestionBank(
  id: number,
  data: UpdateQuestionBankDto
): Promise<QuestionBank> {
  return request.put<QuestionBank>(`/questionbanks/${id}`, data).then(response => response.data)
}

/**
 * 删除题库
 */
export function deleteQuestionBank(id: number): Promise<{ message: string }> {
  return request.delete<{ message: string }>(`/questionbanks/${id}`).then(response => response.data)
}

/**
 * 导出题库（返回文件URL）
 */
export function exportQuestionBank(id: number): string {
  return `/api/questionbanks/${id}/export`
}

/**
 * 导出题库并下载文件
 */
export async function exportQuestionBankFile(id: number): Promise<void> {
  const url = exportQuestionBank(id)
  const response = await fetch(url, {
    headers: {
      'Authorization': `Bearer ${localStorage.getItem('token')}`
    }
  })

  if (!response.ok) {
    throw new Error('导出失败')
  }

  const blob = await response.blob()
  const downloadUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = downloadUrl
  link.download = `questionbank_${new Date().getTime()}.json`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(downloadUrl)
}
