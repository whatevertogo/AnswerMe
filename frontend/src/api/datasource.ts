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
 * GET /api/datasource
 */
export const getDataSourcesApi = (): Promise<DataSource[]> => {
  return request.get<DataSource[]>('/datasource').then(response => response.data)
}

/**
 * 根据ID获取数据源
 * GET /api/datasource/:id
 */
export const getDataSourceApi = (id: number): Promise<DataSource> => {
  return request.get<DataSource>(`/datasource/${id}`).then(response => response.data)
}

/**
 * 获取默认数据源
 * GET /api/datasource/default
 */
export const getDefaultDataSourceApi = (): Promise<DataSource> => {
  return request.get<DataSource>('/datasource/default').then(response => response.data)
}

/**
 * 创建数据源
 * POST /api/datasource
 */
export const createDataSourceApi = (params: CreateDataSourceParams): Promise<DataSource> => {
  return request.post<DataSource>('/datasource', params).then(response => response.data)
}

/**
 * 更新数据源
 * PUT /api/datasource/:id
 */
export const updateDataSourceApi = (id: number, params: UpdateDataSourceParams): Promise<DataSource> => {
  return request.put<DataSource>(`/datasource/${id}`, params).then(response => response.data)
}

/**
 * 删除数据源
 * DELETE /api/datasource/:id
 */
export const deleteDataSourceApi = (id: number): Promise<{ message: string }> => {
  return request.delete<{ message: string }>(`/datasource/${id}`).then(response => response.data)
}

/**
 * 设置默认数据源
 * POST /api/datasource/:id/set-default
 */
export const setDefaultDataSourceApi = (id: number): Promise<{ message: string }> => {
  return request.post<{ message: string }>(`/datasource/${id}/set-default`).then(response => response.data)
}

/**
 * 验证API密钥
 * POST /api/datasource/:id/validate
 */
export const validateApiKeyApi = (id: number): Promise<{ message: string; valid: boolean }> => {
  return request.post<{ message: string; valid: boolean }>(`/datasource/${id}/validate`).then(response => response.data)
}
