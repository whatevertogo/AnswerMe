import { defineStore } from 'pinia'
import { ref } from 'vue'

export interface AIConfig {
  id: string
  name: string
  provider: string
  apiKey: string
  apiUrl?: string
  model?: string
  isDefault: boolean
}

export const useAIConfigStore = defineStore('aiConfig', () => {
  const configs = ref<AIConfig[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  async function fetchConfigs() {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // const response = await api.get('/ai-configs')
      // configs.value = response.data
    } catch {
      error.value = 'Failed to fetch AI configs'
    } finally {
      loading.value = false
    }
  }

  async function addConfig(config: Omit<AIConfig, 'id'>) {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // const response = await api.post('/ai-configs', config)
      // configs.value.push(response.data)
      void config // 占位符，避免未使用参数警告
    } catch {
      error.value = 'Failed to add AI config'
    } finally {
      loading.value = false
    }
  }

  async function deleteConfig(id: string) {
    loading.value = true
    error.value = null
    try {
      // TODO: 实现API调用
      // await api.delete(`/ai-configs/${id}`)
      // configs.value = configs.value.filter(c => c.id !== id)
      void id // 占位符，避免未使用参数警告
    } catch {
      error.value = 'Failed to delete AI config'
    } finally {
      loading.value = false
    }
  }

  return {
    configs,
    loading,
    error,
    fetchConfigs,
    addConfig,
    deleteConfig
  }
})
