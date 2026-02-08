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
export const getDataSourcesApi = () => {
  return request<DataSource[]>({
    url: '/datasource',
    method: 'GET'
  })
}

/**
 * 根据ID获取数据源
 */
export const getDataSourceApi = (id: number) => {
  return request<DataSource>({
    url: `/datasource/${id}`,
    method: 'GET'
  })
}

/**
 * 获取默认数据源
 */
export const getDefaultDataSourceApi = () => {
  return request<DataSource>({
    url: '/datasource/default',
    method: 'GET'
  })
}

/**
 * 创建数据源
 */
export const createDataSourceApi = (params: CreateDataSourceParams) => {
  return request<DataSource>({
    url: '/datasource',
    method: 'POST',
    data: params
  })
}

/**
 * 更新数据源
 */
export const updateDataSourceApi = (id: number, params: UpdateDataSourceParams) => {
  return request<DataSource>({
    url: `/datasource/${id}`,
    method: 'PUT',
    data: params
  })
}

/**
 * 删除数据源
 */
export const deleteDataSourceApi = (id: number) => {
  return request<{ message: string }>({
    url: `/datasource/${id}`,
    method: 'DELETE'
  })
}

/**
 * 设置默认数据源
 */
export const setDefaultDataSourceApi = (id: number) => {
  return request<{ message: string }>({
    url: `/datasource/${id}/set-default`,
    method: 'POST'
  })
}

/**
 * 验证API密钥
 */
export const validateApiKeyApi = (id: number) => {
  return request<{ message: string; valid: boolean }>({
    url: `/datasource/${id}/validate`,
    method: 'POST'
  })
}
