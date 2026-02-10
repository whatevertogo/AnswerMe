export interface AIProviderOption {
  label: string
  value: string
  defaultEndpoint?: string
  defaultModel?: string
  tagType?: 'success' | 'primary' | 'warning' | 'info' | 'danger'
}

export const aiProviders: AIProviderOption[] = [
  {
    label: 'OpenAI',
    value: 'openai',
    defaultModel: 'gpt-3.5-turbo',
    defaultEndpoint: 'https://api.openai.com/v1/chat/completions',
    tagType: 'success'
  },
  {
    label: '通义千问',
    value: 'qwen',
    defaultModel: 'qwen-turbo',
    defaultEndpoint: 'https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions',
    tagType: 'primary'
  },
  {
    label: '智谱GLM',
    value: 'zhipu',
    defaultModel: 'glm-4',
    defaultEndpoint: 'https://open.bigmodel.cn/api/paas/v4/chat/completions',
    tagType: 'warning'
  },
  {
    label: 'Minimax',
    value: 'minimax',
    defaultModel: 'abab6.5s-chat',
    defaultEndpoint: 'https://api.minimax.chat/v1/text/chatcompletion_v2',
    tagType: 'info'
  },
  {
    label: '自定义API (OpenAI兼容)',
    value: 'custom_api',
    defaultEndpoint: 'https://your-api-endpoint.com/v1/chat/completions',
    tagType: 'info'
  }
]

export const getProviderByValue = (value: string): AIProviderOption | undefined => {
  return aiProviders.find(provider => provider.value === value)
}

export const getProviderLabel = (value: string): string => {
  return getProviderByValue(value)?.label ?? value
}

export const getProviderTagType = (value: string): AIProviderOption['tagType'] => {
  return getProviderByValue(value)?.tagType ?? 'info'
}

export const getProviderDefaultEndpoint = (value: string): string => {
  return getProviderByValue(value)?.defaultEndpoint ?? ''
}

export const getProviderDefaultModel = (value: string): string => {
  return getProviderByValue(value)?.defaultModel ?? ''
}
