export interface AIProviderOption {
  label: string
  value: string
  defaultEndpoint?: string
  defaultModel?: string
  tagType?: 'success' | 'primary' | 'warning' | 'info' | 'danger'
  description?: string
}

export const aiProviders: AIProviderOption[] = [
  {
    label: 'OpenAI',
    value: 'openai',
    defaultModel: 'gpt-5.2',
    defaultEndpoint: 'https://api.openai.com/v1/chat/completions',
    tagType: 'success'
  },
  {
    label: 'DeepSeek',
    value: 'deepseek',
    defaultModel: 'deepseek-chat',
    defaultEndpoint: 'https://api.deepseek.com/chat/completions',
    tagType: 'success',
    description: 'V3.2 (128K context, 32K max output)'
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
    label: 'Minimax(中文)',
    value: 'minimax_cn',
    defaultModel: 'M2-her',
    defaultEndpoint: 'https://api.minimaxi.com/v1/text/chatcompletion_v2',
    tagType: 'info'
  },
  {
    label: 'Minimax(英文)',
    value: 'minimax_global',
    defaultModel: 'M2-her',
    defaultEndpoint: 'https://api.minimax.io/v1/text/chatcompletion_v2',
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
