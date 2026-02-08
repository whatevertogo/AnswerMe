import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { DataSource, CreateDataSourceParams, UpdateDataSourceParams } from '@/api/datasource'
import {
  getDataSourcesApi,
  createDataSourceApi,
  updateDataSourceApi,
  deleteDataSourceApi,
  setDefaultDataSourceApi,
  validateApiKeyApi
} from '@/api/datasource'

export const useDataSourceStore = defineStore('dataSource', () => {
  // State
  const dataSources = ref<DataSource[]>([])
  const loading = ref(false)
  const currentDataSource = ref<DataSource | null>(null)

  // Actions
  async function fetchDataSources() {
    loading.value = true
    try {
      dataSources.value = await getDataSourcesApi()
    } catch (error) {
      console.error('获取数据源列表失败:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function createDataSource(params: CreateDataSourceParams) {
    loading.value = true
    try {
      const result = await createDataSourceApi(params)
      dataSources.value.push(result)
      return result
    } catch (error) {
      console.error('创建数据源失败:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function updateDataSource(id: number, params: UpdateDataSourceParams) {
    loading.value = true
    try {
      const result = await updateDataSourceApi(id, params)
      const index = dataSources.value.findIndex(ds => ds.id === id)
      if (index !== -1) {
        dataSources.value[index] = result
      }
      return result
    } catch (error) {
      console.error('更新数据源失败:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function deleteDataSource(id: number) {
    loading.value = true
    try {
      await deleteDataSourceApi(id)
      dataSources.value = dataSources.value.filter(ds => ds.id !== id)
    } catch (error) {
      console.error('删除数据源失败:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function setDefault(id: number) {
    loading.value = true
    try {
      await setDefaultDataSourceApi(id)
      // 更新所有数据源的默认状态
      dataSources.value.forEach(ds => {
        ds.isDefault = ds.id === id
      })
    } catch (error) {
      console.error('设置默认数据源失败:', error)
      throw error
    } finally {
      loading.value = false
    }
  }

  async function validateApiKey(id: number) {
    try {
      const result = await validateApiKeyApi(id)
      return result.valid
    } catch (error) {
      console.error('验证API密钥失败:', error)
      throw error
    }
  }

  function setCurrentDataSource(dataSource: DataSource | null) {
    currentDataSource.value = dataSource
  }

  return {
    dataSources,
    loading,
    currentDataSource,
    fetchDataSources,
    createDataSource,
    updateDataSource,
    deleteDataSource,
    setDefault,
    validateApiKey,
    setCurrentDataSource
  }
})
