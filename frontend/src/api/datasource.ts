import request from '@/utils/request'

export interface DataSource {
  id: number
  userId: number
  name: string
  type: string
  endpoint?: string
  model?: string
  maskedApiKey: string
  isDefault: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateDataSourceParams {
  name: string
  type: string
  apiKey: string
  endpoint?: string
  model?: string
  isDefault?: boolean
}

export interface UpdateDataSourceParams {
  name?: string
  apiKey?: string
  endpoint?: string
  model?: string
  isDefault?: boolean
}

/**
 * 获取当前用户的数据源列表
 */
export const getDataSourcesApi = (): Promise<DataSource[]> => {
  return request.get<DataSource[]>('/datasource')
}

/**
 * 根据ID获取数据源
 */
export const getDataSourceApi = (id: number): Promise<DataSource> => {
  return request.get<DataSource>(`/datasource/${id}`)
}

/**
 * 获取默认数据源
 */
export const getDefaultDataSourceApi = (): Promise<DataSource> => {
  return request.get<DataSource>('/datasource/default')
}

/**
 * 创建数据源
 */
export const createDataSourceApi = (params: CreateDataSourceParams): Promise<DataSource> => {
  return request.post<DataSource>('/datasource', params)
}

/**
 * 更新数据源
 */
export const updateDataSourceApi = (id: number, params: UpdateDataSourceParams): Promise<DataSource> => {
  return request.put<DataSource>(`/datasource/${id}`, params)
}

/**
 * 删除数据源
 */
export const deleteDataSourceApi = (id: number): Promise<{ message: string }> => {
  return request.delete<{ message: string }>(`/datasource/${id}`)
}

/**
 * 设置默认数据源
 */
export const setDefaultDataSourceApi = (id: number): Promise<{ message: string }> => {
  return request.post<{ message: string }>(`/datasource/${id}/set-default`)
}

/**
 * 验证API密钥
 */
export const validateApiKeyApi = (id: number): Promise<{ message: string; valid: boolean }> => {
  return request.post<{ message: string; valid: boolean }>(`/datasource/${id}/validate`)
}
